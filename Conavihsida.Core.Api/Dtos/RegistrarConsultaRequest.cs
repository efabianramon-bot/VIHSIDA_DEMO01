using System.Collections.Generic;

namespace Conavihsida.Core.Api.Dtos
{
    /// <summary>Cuerpo de POST /api/clinica/expediente/{codigo}/consultas.</summary>
    public class RegistrarConsultaRequest
    {
        public string DiagnosticoDetalle { get; set; }
        public string NotasClinicas { get; set; }
        public decimal? CargaViral { get; set; }
        public int? ConteoCd4 { get; set; }

        /// <summary>Recetas TARV emitidas en esta consulta. Puede ir vacio.</summary>
        public List<RecetaRequest> Recetas { get; set; }
    }

    public class RecetaRequest
    {
        public int MedicamentoId { get; set; }
        public string EsquemaTarv { get; set; }
        public int CantidadPrescrita { get; set; }
    }
}
