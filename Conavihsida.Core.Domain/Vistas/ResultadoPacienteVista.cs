using System;

namespace Conavihsida.Core.Domain.Vistas
{
    /// <summary>
    /// Lo unico que el paciente puede ver de un resultado de laboratorio.
    /// A proposito NO tiene ningun campo de valor clinico (RN-01).
    /// Se llena desde la vista SQL vw_resultados_paciente.
    /// </summary>
    public class ResultadoPacienteVista
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string Prueba { get; set; }
        public DateTime FechaSolicitud { get; set; }

        /// <summary>"Listo" o "Pendiente".</summary>
        public string Estado { get; set; }
        public string Nota { get; set; }
    }
}
