namespace Conavihsida.Core.Api.Dtos
{
    /// <summary>Cuerpo de POST /api/clinica/expediente.</summary>
    public class AbrirExpedienteRequest
    {
        public int PacienteId { get; set; }
        public int? MedicoResponsableId { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/clinica/expediente/{codigo}.</summary>
    public class ActualizarExpedienteRequest
    {
        public int? MedicoResponsableId { get; set; }

        /// <summary>Activo, Inactivo o Transferido.</summary>
        public string Estado { get; set; }
    }

    /// <summary>Cuerpo de POST /api/clinica/expediente/{codigo}/resultados.</summary>
    public class SolicitarPruebaRequest
    {
        public string Prueba { get; set; }

        /// <summary>Opcional. Lo unico de texto libre que vera el paciente:
        /// no debe llevar valores clinicos.</summary>
        public string NotaPaciente { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/clinica/resultados/{resultadoId}.</summary>
    public class PublicarResultadoRequest
    {
        /// <summary>Dato clinico: nunca sale hacia el Portal del paciente.</summary>
        public string ValorResultado { get; set; }

        /// <summary>Opcional. Si no viene, se conserva la nota de la solicitud.</summary>
        public string NotaPaciente { get; set; }
    }
}
