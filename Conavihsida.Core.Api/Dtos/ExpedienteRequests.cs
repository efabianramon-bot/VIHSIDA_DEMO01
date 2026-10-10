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
}
