using System;

namespace Conavihsida.Core.Api.Dtos
{
    /// <summary>Cuerpo de POST /api/farmacia/lotes.</summary>
    public class RegistrarLoteRequest
    {
        public int MedicamentoId { get; set; }
        public string NumeroLote { get; set; }
        public DateTime FechaCaducidad { get; set; }

        /// <summary>Unidades recibidas; queda como existencia inicial del lote.</summary>
        public int Cantidad { get; set; }
        public int ExistenciaMinima { get; set; }
        public int? ExistenciaMaxima { get; set; }
    }
}
