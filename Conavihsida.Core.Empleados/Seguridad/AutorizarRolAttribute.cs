using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Conavihsida.Core.Empleados.Seguridad
{
    /// <summary>
    /// Exige sesion iniciada y, si se indican roles, alguno de ellos.
    /// Sin sesion manda al login; con sesion pero sin el rol muestra
    /// "sin acceso" (403) en vez de volver al login, que confundiria.
    ///
    /// Esto solo decide que pantallas se muestran. La proteccion real de
    /// los datos esta en la API, que vuelve a comprobar el rol en cada
    /// llamada (RN-01).
    /// Uso: [AutorizarRol(NombresRol.Medico)]
    /// </summary>
    public class AutorizarRolAttribute : AuthorizeAttribute
    {
        private readonly string[] _rolesPermitidos;

        public AutorizarRolAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos ?? new string[0];
        }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var sesion = EmpleadoPrincipal.SesionActual(httpContext);
            if (sesion == null)
            {
                return false;
            }

            return _rolesPermitidos.Length == 0 || _rolesPermitidos.Any(httpContext.User.IsInRole);
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext filterContext)
        {
            if (EmpleadoPrincipal.SesionActual(filterContext.HttpContext) == null)
            {
                base.HandleUnauthorizedRequest(filterContext);
                return;
            }

            filterContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;
            filterContext.Result = new ViewResult { ViewName = "~/Views/Cuenta/SinAcceso.cshtml" };
        }
    }
}
