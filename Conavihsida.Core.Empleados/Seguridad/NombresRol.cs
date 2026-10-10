namespace Conavihsida.Core.Empleados.Seguridad
{
    /// <summary>
    /// Nombres de rol tal como los devuelve la API (RF-CORE-01). Deben
    /// coincidir con NombresRol de Conavihsida.Core.Domain. No se llama
    /// "Roles" porque chocaria con System.Web.Security.Roles en las vistas.
    /// </summary>
    public static class NombresRol
    {
        public const string Administrador = "Administrador";
        public const string Medico = "Medico";
        public const string Farmaceutico = "Farmaceutico";
        public const string Auditor = "Auditor";
        public const string Cajero = "Cajero";

        /// <summary>El rol como se le muestra al empleado, con sus acentos.</summary>
        public static string Mostrar(string rol)
        {
            switch (rol)
            {
                case Medico: return "Médico";
                case Farmaceutico: return "Farmacéutico";
                default: return rol;
            }
        }
    }
}
