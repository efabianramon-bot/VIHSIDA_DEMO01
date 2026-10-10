using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-05: trazabilidad desde la capa de aplicacion (sin triggers).
    /// Los demas servicios llaman a este despues de cada operacion critica.
    /// </summary>
    public class AuditoriaService
    {
        private const int MaximoPorDefecto = 100;
        private const int MaximoPermitido = 500;

        private readonly IAuditoriaRepository _repositorio;

        public AuditoriaService(IAuditoriaRepository repositorio)
        {
            _repositorio = repositorio;
        }

        public void Registrar(int? usuarioId, int? pacienteId, string terminal,
                              string accion, string entidadAfectada, int? entidadId,
                              string detalle)
        {
            _repositorio.Registrar(new RegistroAuditoria
            {
                UsuarioId = usuarioId,
                PacienteId = pacienteId,
                Terminal = terminal,
                Accion = accion,
                EntidadAfectada = entidadAfectada,
                EntidadId = entidadId,
                FechaHora = DateTime.Now,
                Detalle = detalle
            });
        }

        /// <summary>
        /// Bitacora para el rol Auditor, lo mas reciente primero. "hasta"
        /// es inclusivo por dia: 2026-10-09 incluye todo ese dia.
        /// </summary>
        public List<RegistroAuditoria> Consultar(DateTime? desde, DateTime? hasta, int? usuarioId,
                                                 string accion, int? maximo)
        {
            int limite = Math.Min(Math.Max(maximo ?? MaximoPorDefecto, 1), MaximoPermitido);
            DateTime? hastaExclusivo = hasta == null ? (DateTime?)null : hasta.Value.Date.AddDays(1);
            accion = string.IsNullOrWhiteSpace(accion) ? null : accion.Trim();

            return _repositorio.Consultar(desde, hastaExclusivo, usuarioId, accion, limite);
        }
    }
}
