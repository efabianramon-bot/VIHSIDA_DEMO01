using System;

namespace Conavihsida.Core.Api.Dtos
{
    public class AgendarCitaRequest
    {
        public int PacienteId { get; set; }
        public int MedicoId { get; set; }
        public DateTime Fecha { get; set; }
        public string Hora { get; set; }
        public string Motivo { get; set; }
    }
}
