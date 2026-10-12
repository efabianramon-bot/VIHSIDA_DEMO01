using System;
using Conavihsida.Core.Domain.Catalogos;

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

        /// <summary>
        /// Cuando registro Caja la entrega de esta receta; null si todavia
        /// no (por ejemplo, porque la terminal esta sin conexion). Solo
        /// lectura: sale de las transacciones que sincroniza Caja.
        /// </summary>
        public DateTime? FechaDescargo { get; set; }

        public bool DescargadaEnCaja
        {
            get { return FechaDescargo != null; }
        }

        /// <summary>Hasta cuando se puede despachar (ver ReglasReceta.DiasVigencia).</summary>
        public DateTime FechaVencimiento
        {
            get { return FechaEmision.AddDays(ReglasReceta.DiasVigencia); }
        }
    }
}
