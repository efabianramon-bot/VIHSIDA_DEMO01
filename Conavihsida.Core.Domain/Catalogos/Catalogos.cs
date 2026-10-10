namespace Conavihsida.Core.Domain.Catalogos
{
    // Los valores DEBEN coincidir exactamente con los CHECK del esquema SQL.

    public static class NombresRol
    {
        public const string Administrador = "Administrador";
        public const string Medico = "Medico";
        public const string Farmaceutico = "Farmaceutico";
        public const string Auditor = "Auditor";
        public const string Cajero = "Cajero";
    }

    public static class OrigenesAcceso
    {
        public const string Web = "WEB";
        public const string Caja = "CAJA";
        public const string Core = "CORE";

        public static bool EsValido(string valor)
        {
            return valor == Web || valor == Caja || valor == Core;
        }
    }

    public static class EstadosCita
    {
        public const string Confirmada = "Confirmada";
        public const string Pendiente = "Pendiente";
        public const string Cancelada = "Cancelada";
    }

    public static class EstadosFactura
    {
        public const string Pendiente = "Pendiente";
        public const string Pagada = "Pagada";
    }

    public static class FormasPago
    {
        public const string Tarjeta = "Tarjeta";
        public const string Efectivo = "Efectivo";
        public const string Transferencia = "Transferencia";
        public const string Subsidio = "Subsidio";

        public static bool EsValida(string valor)
        {
            return valor == Tarjeta || valor == Efectivo
                || valor == Transferencia || valor == Subsidio;
        }
    }

    public static class EstadosReceta
    {
        public const string Activa = "Activa";
        public const string Dispensada = "Dispensada";
        public const string Vencida = "Vencida";
        public const string Cancelada = "Cancelada";
    }

    public static class EstadosResultado
    {
        public const string Pendiente = "Pendiente";
        public const string Listo = "Listo";
    }

    public static class TiposMovimiento
    {
        public const string Entrada = "Entrada";
        public const string Salida = "Salida";
    }
}
