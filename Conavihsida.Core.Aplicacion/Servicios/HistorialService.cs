using System;
using System.Collections.Generic;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-03: consultas, evolucion, carga viral / CD4 y recetas TARV.
    /// Respalda /api/clinica/expediente/{codigo}/historial y /consultas,
    /// de uso exclusivo del rol Medico (RN-01).
    /// </summary>
    public class HistorialService
    {
        public const string MensajeExpedienteNoExiste = "El expediente no existe.";

        private const int LargoMaximoEsquema = 150;

        private readonly IHistorialRepository _historial;
        private readonly IExpedienteRepository _expedientes;
        private readonly AuditoriaService _auditoria;

        public HistorialService(IHistorialRepository historial, IExpedienteRepository expedientes,
                                AuditoriaService auditoria)
        {
            _historial = historial;
            _expedientes = expedientes;
            _auditoria = auditoria;
        }

        /// <summary>null si el expediente no existe.</summary>
        public List<HistorialClinico> ObtenerHistorial(string codigoSerologico, int usuarioId, string terminal)
        {
            if (_expedientes.ObtenerPorCodigo(codigoSerologico) == null)
            {
                return null;
            }

            var consultas = _historial.ObtenerPorExpediente(codigoSerologico);
            Auditar(usuarioId, terminal, "ConsultarHistorial", null, codigoSerologico);
            return consultas;
        }

        /// <summary>
        /// Registra una consulta con su evolucion, los valores de carga viral
        /// y CD4 si los hay, y las recetas TARV que el medico emita en ella.
        /// Todo se guarda en una sola transaccion.
        /// </summary>
        public ResultadoOperacion<HistorialClinico> RegistrarConsulta(
            string codigoSerologico, int usuarioMedicoId, decimal? cargaViral, int? conteoCd4,
            string diagnosticoDetalle, string notasClinicas, List<Receta> recetas, string terminal)
        {
            recetas = recetas ?? new List<Receta>();
            diagnosticoDetalle = Limpiar(diagnosticoDetalle);
            notasClinicas = Limpiar(notasClinicas);

            if (diagnosticoDetalle == null && notasClinicas == null
                && cargaViral == null && conteoCd4 == null && recetas.Count == 0)
            {
                return Fallo("La consulta está vacía: indica diagnóstico, notas, resultados o una receta.");
            }

            if (cargaViral < 0 || conteoCd4 < 0)
            {
                return Fallo("La carga viral y el conteo de CD4 no pueden ser negativos.");
            }

            var ahora = DateTime.Now;
            foreach (var receta in recetas)
            {
                if (receta.MedicamentoId <= 0)
                {
                    return Fallo("Cada receta debe indicar el medicamento.");
                }

                if (receta.CantidadPrescrita <= 0)
                {
                    return Fallo("La cantidad prescrita debe ser mayor que cero.");
                }

                receta.EsquemaTarv = Limpiar(receta.EsquemaTarv);
                if (receta.EsquemaTarv != null && receta.EsquemaTarv.Length > LargoMaximoEsquema)
                {
                    return Fallo("El esquema TARV admite máximo 150 caracteres.");
                }

                receta.FechaEmision = ahora;
                receta.Estado = EstadosReceta.Activa;
            }

            var expediente = _expedientes.ObtenerPorCodigo(codigoSerologico);
            if (expediente == null)
            {
                return Fallo(MensajeExpedienteNoExiste);
            }

            if (expediente.Estado != EstadosExpediente.Activo)
            {
                return Fallo("El expediente no está activo; no admite nuevas consultas.");
            }

            var consulta = new HistorialClinico
            {
                CodigoSerologico = codigoSerologico,
                UsuarioMedicoId = usuarioMedicoId,
                FechaConsulta = ahora,
                CargaViral = cargaViral,
                ConteoCd4 = conteoCd4,
                DiagnosticoDetalle = diagnosticoDetalle,
                NotasClinicas = notasClinicas,
                Recetas = recetas
            };

            if (!_historial.RegistrarConsulta(consulta))
            {
                return Fallo("No se guardó la consulta. Verifica que los medicamentos recetados existan.");
            }

            // Ojo: en la auditoria NO van diagnostico, notas ni valores
            // clinicos (RN-01); solo que se hizo, quien y sobre que registro.
            Auditar(usuarioMedicoId, terminal, "RegistrarConsulta", consulta.Id, codigoSerologico);
            foreach (var receta in recetas)
            {
                Auditar(usuarioMedicoId, terminal, "EmitirReceta", receta.Id, codigoSerologico);
            }

            return ResultadoOperacion<HistorialClinico>.Ok(consulta);
        }

        private void Auditar(int usuarioId, string terminal, string accion, int? entidadId, string codigoSerologico)
        {
            string entidad = accion == "EmitirReceta" ? "recetas" : "historial_clinico";
            _auditoria.Registrar(usuarioId, null, terminal, accion, entidad, entidadId,
                "codigo=" + codigoSerologico);
        }

        private static ResultadoOperacion<HistorialClinico> Fallo(string mensaje)
        {
            return ResultadoOperacion<HistorialClinico>.Fallo(mensaje);
        }

        private static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
    }
}
