using System;

namespace Conavihsida.Core.Domain.Entidades
{
    // Lo que el modulo de Caja sincroniza con el CORE. Los nombres de las
    // propiedades son los del contrato de Caja (docs/CONTRATO_API.md de su
    // repositorio): la API los recibe tal cual, asi que no hay que
    // cambiarlos sin acordarlo con Caja e Integracion.
    //
    // IdLocal es el Id de la base SQLite de la terminal: se guarda como
    // referencia, pero la clave en el CORE es el numero de negocio.

    /// <summary>Tabla: CajaTurnos. Clave: NumeroTurno.</summary>
    public class TurnoCaja
    {
        public int IdLocal { get; set; }
        public string NumeroTurno { get; set; }
        public string UsuarioCajero { get; set; }
        public string NombreCajero { get; set; }
        public string Caja { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal BalanceInicial { get; set; }
        public decimal SaldoEsperado { get; set; }
        public decimal? EfectivoDeclarado { get; set; }
        public decimal? Diferencia { get; set; }

        /// <summary>Abierto o Cerrado.</summary>
        public string Estado { get; set; }
        public string Observacion { get; set; }
    }

    /// <summary>Tabla: CajaMovimientos. Clave: NumeroMovimiento.</summary>
    public class MovimientoCaja
    {
        public int IdLocal { get; set; }
        public int TurnoCajaIdLocal { get; set; }
        public string NumeroMovimiento { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Entrada, Salida, Cobro, Subsidio o Ajuste.</summary>
        public string Tipo { get; set; }
        public decimal Monto { get; set; }
        public string Concepto { get; set; }

        /// <summary>Numero de la transaccion a la que corresponde, si la hay.</summary>
        public string Referencia { get; set; }
        public string UsuarioCajero { get; set; }
    }

    /// <summary>Tabla: CajaTransacciones. Clave: NumeroTransaccion.</summary>
    public class TransaccionCaja
    {
        public int IdLocal { get; set; }
        public string NumeroTransaccion { get; set; }
        public DateTime Fecha { get; set; }
        public string CedulaPaciente { get; set; }
        public string TipoServicio { get; set; }
        public decimal Monto { get; set; }

        /// <summary>Efectivo, Tarjeta o Subsidiado.</summary>
        public string FormaPago { get; set; }
        public bool EsSubsidiado { get; set; }

        /// <summary>Pendiente, Completada o Anulada.</summary>
        public string Estado { get; set; }
        public bool ComprobanteGenerado { get; set; }
        public string UsuarioCajero { get; set; }

        /// <summary>Opcional: la receta que se descarga con esta entrega subsidiada.</summary>
        public int? NumeroReceta { get; set; }
    }

    /// <summary>
    /// La factura del paciente que se creo o se anulo al sincronizar una
    /// transaccion de Caja (Facturas.TransaccionCajaID).
    /// </summary>
    public class FacturaDeCaja
    {
        public int FacturaId { get; set; }
        public int PacienteId { get; set; }

        /// <summary>null cuando la factura se anulo.</summary>
        public string NumeroRecibo { get; set; }
    }

    /// <summary>Tabla: CajaComprobantes. Clave: NumeroComprobante. Solo metadatos: el PDF queda en la terminal.</summary>
    public class ComprobanteCaja
    {
        public int IdLocal { get; set; }
        public int TransaccionIdLocal { get; set; }
        public string NumeroComprobante { get; set; }
        public DateTime FechaGeneracion { get; set; }
        public string TipoDocumento { get; set; }
        public bool EsReimpresion { get; set; }
        public int CantidadReimpresiones { get; set; }
    }

    /// <summary>Tabla: CajaFondos. Clave: Codigo.</summary>
    public class FondoConvenio
    {
        public int IdLocal { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }

        /// <summary>PresupuestoEstatal o DonacionInternacional.</summary>
        public string TipoFuente { get; set; }
        public string Organismo { get; set; }
        public decimal MontoAsignado { get; set; }
        public decimal MontoUtilizado { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public bool Activo { get; set; }
    }
}
