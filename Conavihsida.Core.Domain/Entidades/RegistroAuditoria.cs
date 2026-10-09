using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>RF-CORE-05: trazabilidad hecha desde la capa de aplicacion (sin triggers).</summary>
    public class RegistroAuditoria
    {
        public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public int? PacienteId { get; set; }
        public string Terminal { get; set; }
        public string Accion { get; set; }
        public string EntidadAfectada { get; set; }
        public int? EntidadId { get; set; }
        public DateTime FechaHora { get; set; }

        /// <summary>Texto libre o JSON. NO incluir datos clinicos ni el motivo de una cita.</summary>
        public string Detalle { get; set; }
    }
}
