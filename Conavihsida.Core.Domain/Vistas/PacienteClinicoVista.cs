using System;

namespace Conavihsida.Core.Domain.Vistas
{
    /// <summary>
    /// Identidad civil de un paciente junto con el codigo de su expediente,
    /// si lo tiene. Es la union que RF-CORE-02 mantiene separada: solo se
    /// entrega al rol Medico y cada consulta queda en la bitacora (RN-01).
    /// No lleva ningun dato clinico.
    /// </summary>
    public class PacienteClinicoVista
    {
        public int PacienteId { get; set; }
        public string NombreCompleto { get; set; }
        public string Cedula { get; set; }
        public DateTime? FechaNacimiento { get; set; }

        /// <summary>null si el paciente todavia no tiene expediente.</summary>
        public string CodigoSerologico { get; set; }
        public string EstadoExpediente { get; set; }
    }
}
