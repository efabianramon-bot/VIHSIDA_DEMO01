using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Contiene el valor clinico real (ValorResultado). RN-01: esta
    /// entidad NUNCA debe salir hacia el Portal del paciente; para eso
    /// existe ResultadoPacienteVista.
    /// </summary>
    public class ResultadoLab
    {
        public int Id { get; set; }
        public string CodigoSerologico { get; set; }
        public string Prueba { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; }
        public string ValorResultado { get; set; }
        public DateTime? FechaResultado { get; set; }
        public string NotaPaciente { get; set; }
    }
}
