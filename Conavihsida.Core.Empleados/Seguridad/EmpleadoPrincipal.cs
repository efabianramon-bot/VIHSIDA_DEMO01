using System;
using System.Linq;
using System.Security.Principal;
using System.Web;

namespace Conavihsida.Core.Empleados.Seguridad
{
    /// <summary>El empleado con sesion iniciada, como usuario de la peticion.</summary>
    public class EmpleadoPrincipal : IPrincipal
    {
        public EmpleadoPrincipal(IIdentity identidad, SesionEmpleado sesion)
        {
            Identity = identidad;
            Sesion = sesion;
        }

        public IIdentity Identity { get; private set; }

        public SesionEmpleado Sesion { get; private set; }

        public bool IsInRole(string rol)
        {
            return Sesion.Roles.Contains(rol, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>null si nadie ha iniciado sesion.</summary>
        public static SesionEmpleado SesionActual(HttpContextBase contexto)
        {
            var empleado = contexto.User as EmpleadoPrincipal;
            return empleado == null ? null : empleado.Sesion;
        }
    }
}
