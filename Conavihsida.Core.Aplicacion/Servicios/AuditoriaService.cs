using System;
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
    }
}
