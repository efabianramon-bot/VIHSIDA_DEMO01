using System;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    /// <summary>
    /// Guarda lo que sincroniza Caja. Cada metodo es idempotente: si el
    /// registro ya existe (por su numero de negocio) lo actualiza en vez de
    /// duplicarlo, porque Caja reenvia lo que no sabe si llego y vuelve a
    /// mandar un turno al cerrarlo o una transaccion al anularla.
    /// Devuelven true si el registro era nuevo y false si ya existia.
    /// </summary>
    public interface ICajaRepository
    {
        bool GuardarTurno(TurnoCaja turno, DateTime ahora);

        bool GuardarMovimiento(MovimientoCaja movimiento, DateTime ahora);

        /// <summary>
        /// transaccion.CedulaPaciente debe venir con el formato
        /// 000-0000000-0; con ella se busca el PacienteID (queda NULL si
        /// no hay un paciente con esa cedula). Devuelve null, sin guardar
        /// nada, si NumeroReceta no es una receta de ese mismo paciente:
        /// de ese enlace depende que Farmacia vea la receta como descargada.
        /// </summary>
        bool? GuardarTransaccion(TransaccionCaja transaccion, DateTime ahora);

        /// <summary>
        /// RN-05: en una sola transaccion crea la factura Pagada de esa
        /// transaccion ya guardada, con su pago y su comprobante (REC/FAC).
        /// Devuelve null, sin crear nada, si la transaccion no tiene
        /// paciente registrado o si ya tiene factura (Caja la reenvio).
        /// </summary>
        FacturaDeCaja CrearFacturaDeTransaccion(string numeroTransaccion, string concepto, decimal monto,
                                                bool esSubsidiada, string formaPago, DateTime fechaPago,
                                                DateTime ahora);

        /// <summary>
        /// Pasa a Anulada la factura de esa transaccion. Devuelve null si no
        /// tenia factura o si ya estaba anulada.
        /// </summary>
        FacturaDeCaja AnularFacturaDeTransaccion(string numeroTransaccion);

        bool GuardarComprobante(ComprobanteCaja comprobante, DateTime ahora);

        bool GuardarFondo(FondoConvenio fondo, DateTime ahora);
    }
}
