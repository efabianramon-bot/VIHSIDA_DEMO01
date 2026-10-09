namespace Conavihsida.Core.Aplicacion.Comun
{
    /// <summary>
    /// Forma estandar en la que los servicios responden: o salio bien
    /// (Valor lleno) o fallo (MensajeError lleno). Asi la capa de
    /// Integracion nunca depende de excepciones para casos normales
    /// como "horario ocupado" o "factura ya pagada".
    /// </summary>
    public class ResultadoOperacion<T>
    {
        public bool Exitoso { get; private set; }
        public string MensajeError { get; private set; }
        public T Valor { get; private set; }

        public static ResultadoOperacion<T> Ok(T valor)
        {
            return new ResultadoOperacion<T> { Exitoso = true, Valor = valor };
        }

        public static ResultadoOperacion<T> Fallo(string mensaje)
        {
            return new ResultadoOperacion<T> { Exitoso = false, MensajeError = mensaje };
        }
    }
}
