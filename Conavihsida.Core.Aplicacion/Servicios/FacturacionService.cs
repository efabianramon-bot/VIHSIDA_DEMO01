using System;
using System.Collections.Generic;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Facturas, pago y comprobantes. Respalda /pacientes/{id}/facturas
    /// y /facturas/{id}/pagar del Portal Web.
    ///
    /// IMPORTANTE (PCI-DSS): este servicio nunca recibe numero de tarjeta
    /// ni CVV. La pasarela de pago (Integracion) los procesa y aqui solo
    /// llega la referencia/token que devolvio.
    /// </summary>
    public class FacturacionService
    {
        private readonly IFacturaRepository _facturas;
        private readonly AuditoriaService _auditoria;

        public FacturacionService(IFacturaRepository facturas, AuditoriaService auditoria)
        {
            _facturas = facturas;
            _auditoria = auditoria;
        }

        public List<Factura> ObtenerFacturasPaciente(int pacienteId)
        {
            return _facturas.ObtenerPorPaciente(pacienteId);
        }

        public Factura ObtenerFactura(int pacienteId, int facturaId)
        {
            return _facturas.ObtenerPorId(pacienteId, facturaId);
        }

        /// <param name="cajeroId">null cuando el pago lo hace el paciente desde la Web.</param>
        public ResultadoOperacion<Factura> PagarFactura(int pacienteId, int facturaId,
            string formaPago, string referenciaPasarela, int? cajeroId, string terminal)
        {
            var factura = _facturas.ObtenerPorId(pacienteId, facturaId);
            if (factura == null)
            {
                return ResultadoOperacion<Factura>.Fallo("La factura no existe.");
            }

            if (factura.Estado == EstadosFactura.Pagada)
            {
                return ResultadoOperacion<Factura>.Fallo("Esta factura ya fue pagada.");
            }

            if (factura.EsSubsidiada || factura.Monto == 0m)
            {
                // RN-02: ARV subsidiado al 100%, saldo a cargo del paciente RD$ 0.00.
                formaPago = FormasPago.Subsidio;
            }
            else
            {
                if (!FormasPago.EsValida(formaPago) || formaPago == FormasPago.Subsidio)
                {
                    return ResultadoOperacion<Factura>.Fallo("La forma de pago no es válida.");
                }

                if (formaPago == FormasPago.Tarjeta && string.IsNullOrWhiteSpace(referenciaPasarela))
                {
                    return ResultadoOperacion<Factura>.Fallo("No se recibió la confirmación de la pasarela de pago.");
                }
            }

            // RN-05: pago + recibo + factura se guardan juntos en una sola
            // transaccion (la hace el repositorio). Sin comprobante no hay pago.
            int numero = _facturas.SiguienteNumeroComprobante();
            DateTime ahora = DateTime.Now;

            var pago = new Pago
            {
                FacturaId = factura.Id,
                Monto = factura.Monto,
                FormaPago = formaPago,
                ReferenciaPasarela = referenciaPasarela,
                CajeroId = cajeroId,
                FechaPago = ahora
            };

            var comprobante = new Comprobante
            {
                FacturaId = factura.Id,
                NumeroRecibo = "REC-" + numero,
                NumeroFactura = "FAC-" + numero,
                FechaEmision = ahora
            };

            if (!_facturas.RegistrarPagoConComprobante(pago, comprobante))
            {
                return ResultadoOperacion<Factura>.Fallo("El pago no pudo completarse. Intenta de nuevo.");
            }

            factura.Estado = EstadosFactura.Pagada;
            factura.NumeroRecibo = comprobante.NumeroRecibo;
            factura.NumeroFactura = comprobante.NumeroFactura;
            factura.FechaPago = ahora;

            _auditoria.Registrar(cajeroId, pacienteId, terminal, "PagarFactura", "facturas", factura.Id,
                string.Format("monto={0};formaPago={1};recibo={2}", factura.Monto, formaPago, comprobante.NumeroRecibo));

            return ResultadoOperacion<Factura>.Ok(factura);
        }
    }
}
