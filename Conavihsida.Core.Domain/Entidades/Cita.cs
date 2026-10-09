using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>Tabla: citas.</summary>
    public class Cita
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Formato "HH:mm".</summary>
        public string Hora { get; set; }
        public string Motivo { get; set; }

        /// <summary>Ver Catalogos.EstadosCita.</summary>
        public string Estado { get; set; }
        public DateTime FechaCreacion { get; set; }

        // Solo lectura: se llenan con un JOIN a medicos/usuarios.
        public string MedicoNombre { get; set; }
        public string Especialidad { get; set; }
    }
}
