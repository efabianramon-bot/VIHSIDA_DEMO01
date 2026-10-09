using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>RN-05: recibo + factura, siempre generados juntos. Tabla: comprobantes.</summary>
    public class Comprobante
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public string NumeroRecibo { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime FechaEmision { get; set; }
    }
}
