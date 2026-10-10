using System.Web.Mvc;
using Conavihsida.Core.Empleados.Seguridad;

namespace Conavihsida.Core.Empleados
{
    public static class FilterConfig
    {
        public static void Register(GlobalFilterCollection filters)
        {
            // Todo exige sesion iniciada, salvo lo marcado con [AllowAnonymous]
            // (la pantalla de login). Cada portal agrega ademas su rol.
            filters.Add(new AutorizarRolAttribute());
        }
    }
}
