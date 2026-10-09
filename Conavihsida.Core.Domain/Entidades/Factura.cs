using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>Tabla: facturas.</summary>
    public class Factura
    {
        public int Id { get; set; }
        public int PacienteId { get; set; }
        public string Concepto { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }

        /// <summary>RN-02: si es true, Monto debe ser 0.</summary>
        public bool EsSubsidiada { get; set; }

        /// <summary>Ver Catalogos.EstadosFactura.</summary>
        public string Estado { get; set; }

        // Solo lectura: se llenan con un LEFT JOIN a comprobantes/pagos
        // (son null mientras la factura este Pendiente).
        public string NumeroRecibo { get; set; }
        public string NumeroFactura { get; set; }
        public DateTime? FechaPago { get; set; }
    }
}
