using System;
using System.Collections.Generic;
using System.Linq;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Logica de agenda de citas. Respalda los endpoints /medicos,
    /// /medicos/{id}/horarios, /pacientes/{id}/citas, /citas y
    /// /citas/{id}/cancelar que espera el Portal Web.
    /// </summary>
    public class CitaService
    {
        // Horario base de atencion (igual al mock del portal). Mas
        // adelante puede salir de una tabla horarios_atencion.
        private static readonly string[] HorasBase =
            { "08:00", "09:00", "10:00", "11:00", "14:00", "15:00", "16:00" };

        private const int LargoMaximoMotivo = 300;

        private readonly ICitaRepository _citas;
        private readonly AuditoriaService _auditoria;

        public CitaService(ICitaRepository citas, AuditoriaService auditoria)
        {
            _citas = citas;
            _auditoria = auditoria;
        }

        public List<Medico> ObtenerMedicos()
        {
            return _citas.ObtenerMedicos();
        }

        public List<Cita> ObtenerCitasPaciente(int pacienteId)
        {
            return _citas.ObtenerCitasPaciente(pacienteId);
        }

        public List<string> ObtenerHorariosDisponibles(int medicoId, DateTime fecha)
        {
            if (fecha.Date < DateTime.Today)
            {
                return new List<string>();
            }

            var ocupadas = new HashSet<string>(_citas.ObtenerHorasOcupadas(medicoId, fecha.Date));
            return HorasBase.Where(h => !ocupadas.Contains(h)).ToList();
        }

        public ResultadoOperacion<Cita> AgendarCita(int pacienteId, int medicoId, DateTime fecha,
                                                    string hora, string motivo, string terminal)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ResultadoOperacion<Cita>.Fallo("Cuéntanos brevemente el motivo de la cita.");
            }

            motivo = motivo.Trim();
            if (motivo.Length > LargoMaximoMotivo)
            {
                return ResultadoOperacion<Cita>.Fallo("El motivo admite máximo 300 caracteres.");
            }

            if (fecha.Date < DateTime.Today)
            {
                return ResultadoOperacion<Cita>.Fallo("La fecha no puede ser anterior a hoy.");
            }

            if (!HorasBase.Contains(hora))
            {
                return ResultadoOperacion<Cita>.Fallo("El horario seleccionado no es válido.");
            }

            var medico = _citas.ObtenerMedico(medicoId);
            if (medico == null || !medico.Activo)
            {
                return ResultadoOperacion<Cita>.Fallo("El médico seleccionado no existe.");
            }

            var cita = new Cita
            {
                PacienteId = pacienteId,
                MedicoId = medicoId,
                Fecha = fecha.Date,
                Hora = hora,
                Motivo = motivo,
                Estado = EstadosCita.Confirmada,
                FechaCreacion = DateTime.Now
            };

            // La proteccion real contra doble reserva es el indice unico de
            // la base de datos; el repositorio devuelve null si lo viola.
            int? id = _citas.Insertar(cita);
            if (id == null)
            {
                return ResultadoOperacion<Cita>.Fallo("Ese horario ya no está disponible. Elige otro.");
            }

            cita.Id = id.Value;
            cita.MedicoNombre = medico.NombreCompleto;
            cita.Especialidad = medico.Especialidad;

            // Ojo: en la auditoria NO va el motivo (puede contener datos de salud).
            _auditoria.Registrar(null, pacienteId, terminal, "AgendarCita", "citas", cita.Id,
                string.Format("medicoId={0};fecha={1:yyyy-MM-dd};hora={2}", medicoId, fecha, hora));

            return ResultadoOperacion<Cita>.Ok(cita);
        }

        public bool CancelarCita(int pacienteId, int citaId, string terminal)
        {
            bool cancelada = _citas.Cancelar(citaId, pacienteId);
            if (cancelada)
            {
                _auditoria.Registrar(null, pacienteId, terminal, "CancelarCita", "citas", citaId, null);
            }
            return cancelada;
        }
    }
}
