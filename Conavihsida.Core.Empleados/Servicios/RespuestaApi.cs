namespace Conavihsida.Core.Empleados.Servicios
{
    /// <summary>
    /// Resultado de una llamada a la API: o trae Datos, o trae el Error
    /// que la API devolvio, listo para mostrarse en pantalla.
    /// </summary>
    public class RespuestaApi<T>
    {
        /// <summary>Codigo HTTP; 0 si no se pudo conectar.</summary>
        public int Codigo { get; set; }
        public T Datos { get; set; }
        public string Error { get; set; }

        public bool Exitosa
        {
            get { return Codigo >= 200 && Codigo < 300; }
        }
    }
}
