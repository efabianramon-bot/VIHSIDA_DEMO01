using System;

namespace Conavihsida.Core.Domain.Entidades
{
    public class Receta
    {
        public int Id { get; set; }
        public int HistorialId { get; set; }
        public string CodigoSerologico { get; set; }
        public int MedicamentoId { get; set; }
        public string EsquemaTarv { get; set; }
        public int CantidadPrescrita { get; set; }
        public DateTime FechaEmision { get; set; }

        /// <summary>Ver Catalogos.EstadosReceta.</summary>
        public string Estado { get; set; }
    }
}
