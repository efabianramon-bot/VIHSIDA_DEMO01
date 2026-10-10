using System;
using System.Security.Principal;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using Conavihsida.Core.Empleados.Seguridad;

namespace Conavihsida.Core.Empleados
{
    public class MvcApplication : HttpApplication
    {
        protected void Application_Start()
        {
            MvcHandler.DisableMvcResponseHeader = true;
            // Web.config ya manda X-Frame-Options: DENY; sin esto los
            // formularios agregarian un segundo valor (SAMEORIGIN).
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;
            FilterConfig.Register(GlobalFilters.Filters);
            RouteConfig.Register(RouteTable.Routes);
        }

        // Un formulario enviado con el token de seguridad vencido (por
        // ejemplo, la pantalla de login abierta desde ayer) no es un error
        // del sistema: se manda a empezar de nuevo.
        protected void Application_Error(object sender, EventArgs e)
        {
            if (Server.GetLastError() is HttpAntiForgeryException)
            {
                Server.ClearError();
                Response.Redirect("~/Cuenta/Login");
            }
        }

        // La cookie de sesion trae al empleado y sus roles (ver
        // SesionEmpleado). Aqui se convierte en el usuario de la peticion,
        // para que [AutorizarRol] y User.IsInRole funcionen.
        protected void Application_PostAuthenticateRequest(object sender, EventArgs e)
        {
            var identidad = HttpContext.Current.User == null
                ? null : HttpContext.Current.User.Identity as FormsIdentity;
            if (identidad == null || !identidad.IsAuthenticated)
            {
                return;
            }

            var sesion = SesionEmpleado.Leer(identidad.Ticket);
            if (sesion == null)
            {
                // Cookie con un formato que no entendemos: se trata como anonimo.
                HttpContext.Current.User = new GenericPrincipal(new GenericIdentity(string.Empty), new string[0]);
                return;
            }

            HttpContext.Current.User = new EmpleadoPrincipal(identidad, sesion);
        }
    }
}
