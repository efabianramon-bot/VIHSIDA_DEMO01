using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using NLog;

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
        private static readonly Logger Log = LogManager.GetLogger("CoreApi");

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
                AuditarRechazo(actionContext.Request, identidad);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden,
                    new { error = "Tu rol no tiene acceso a este recurso." });
            }
        }

        // RF-CORE-05: un intento de entrar donde no corresponde tambien es
        // una operacion auditable. Si la bitacora falla, el 403 se devuelve
        // igual: nunca se deja pasar la peticion por un error de auditoria.
        private static void AuditarRechazo(HttpRequestMessage request, IdentidadLlamada identidad)
        {
            try
            {
                string cadena = ConfigurationManager.ConnectionStrings["ConavihsidaCoreDb"].ConnectionString;
                new AuditoriaService(new AuditoriaRepository(cadena)).Registrar(
                    identidad.UsuarioId, null, IdentidadLlamada.LeerTerminal(request),
                    "AccesoDenegado", "api", null,
                    string.Format("ruta={0} {1};roles={2}", request.Method, request.RequestUri.AbsolutePath,
                        string.Join(",", identidad.Roles)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "No se pudo auditar un acceso denegado");
            }
        }
    }
}
