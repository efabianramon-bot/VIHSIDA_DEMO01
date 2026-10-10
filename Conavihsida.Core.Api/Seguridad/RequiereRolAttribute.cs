using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Conavihsida.Core.Api.Seguridad
{
    /// <summary>
    /// RF-CORE-01 (RBAC): solo deja pasar a empleados con alguno de los
    /// roles indicados. Sin identidad de empleado responde 401; con
    /// identidad pero sin el rol responde 403 (criterio de aceptacion #5).
    /// Sin roles en el atributo, basta con ser un empleado autenticado.
    /// Uso: [RequiereRol(NombresRol.Medico)]
    /// </summary>
    public class RequiereRolAttribute : AuthorizationFilterAttribute
    {
        private readonly string[] _rolesPermitidos;

        public RequiereRolAttribute(params string[] rolesPermitidos)
        {
            _rolesPermitidos = rolesPermitidos ?? new string[0];
        }

        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var identidad = IdentidadLlamada.Leer(actionContext.Request);

            if (identidad.UsuarioId == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Falta la identidad del usuario (X-Usuario-Id)." });
                return;
            }

            if (_rolesPermitidos.Length > 0 && !identidad.TieneRol(_rolesPermitidos))
            {
                AuditoriaAccesos.RegistrarRechazo(actionContext.Request, identidad);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden,
                    new { error = "Tu rol no tiene acceso a este recurso." });
            }
        }
    }
}
