using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Tabla: pagos. Jamas guarda numero de tarjeta ni CVV: solo la
    /// referencia/token que devuelve la pasarela de pago.
    /// </summary>
    public class Pago
    {
        public int Id { get; set; }
        public int FacturaId { get; set; }
        public decimal Monto { get; set; }

        /// <summary>Ver Catalogos.FormasPago.</summary>
        public string FormaPago { get; set; }
        public string ReferenciaPasarela { get; set; }

        /// <summary>null si el pago es self-service via Web.</summary>
        public int? CajeroId { get; set; }
        public DateTime FechaPago { get; set; }
    }
}
