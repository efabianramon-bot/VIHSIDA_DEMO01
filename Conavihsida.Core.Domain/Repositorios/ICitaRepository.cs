using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface ICitaRepository
    {
        List<Medico> ObtenerMedicos();

        /// <summary>null si no existe.</summary>
        Medico ObtenerMedico(int medicoId);

        List<Cita> ObtenerCitasPaciente(int pacienteId);

        /// <summary>Horas "HH:mm" ya tomadas (citas no canceladas) de ese medico ese dia.</summary>
        List<string> ObtenerHorasOcupadas(int medicoId, DateTime fecha);

        /// <summary>
        /// Inserta la cita y devuelve su Id. Debe devolver null (sin lanzar
        /// excepcion) cuando el indice unico ux_citas_medico_fecha_hora_activa
        /// rechaza el INSERT por doble reserva.
        /// </summary>
        int? Insertar(Cita cita);

        /// <summary>Cancela la cita solo si pertenece a ese paciente. false si no existe.</summary>
        bool Cancelar(int citaId, int pacienteId);
    }
}
