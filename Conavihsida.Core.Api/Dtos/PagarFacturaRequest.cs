namespace Conavihsida.Core.Api.Dtos
{
    public class PagarFacturaRequest
    {
        public string FormaPago { get; set; }

        /// <summary>Token/referencia que YA devolvio la pasarela de pago.
        /// Nunca mandar numero de tarjeta ni CVV a este endpoint.</summary>
        public string ReferenciaPasarela { get; set; }
    }
}
