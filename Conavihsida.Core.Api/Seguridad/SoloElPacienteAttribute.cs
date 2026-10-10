using System.Net;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Conavihsida.Core.Api.Dtos;

namespace Conavihsida.Core.Api.Seguridad
{
    /// <summary>
    /// Ley 172-13: un paciente solo puede ver y tocar lo suyo. Compara el
    /// paciente que el Gateway identifico (X-Paciente-Id) con el pacienteId
    /// de la peticion, venga en la ruta, en la query o en el cuerpo. Sin
    /// X-Paciente-Id responde 401; si no coinciden responde 403 y queda en
    /// la bitacora. Sin esto bastaba cambiar el numero de la URL para ver
    /// las citas, facturas o resultados de otra persona.
    /// </summary>
    public class SoloElPacienteAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            var identidad = IdentidadLlamada.Leer(actionContext.Request);

            if (identidad.PacienteId == null)
            {
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Falta la identidad del paciente (X-Paciente-Id)." });
                return;
            }

            int? solicitado = PacienteSolicitado(actionContext);
            if (solicitado == null)
            {
                // No hay cuerpo que revisar: la accion respondera 400.
                return;
            }

            if (solicitado != identidad.PacienteId)
            {
                AuditoriaAccesos.RegistrarRechazo(actionContext.Request, identidad);
                actionContext.Response = actionContext.Request.CreateResponse(HttpStatusCode.Forbidden,
                    new { error = "Solo puedes acceder a tu propia información." });
            }
        }

        private static int? PacienteSolicitado(HttpActionContext actionContext)
        {
            object valor;
            if (actionContext.ActionArguments.TryGetValue("pacienteId", out valor) && valor is int)
            {
                return (int)valor;
            }

            // POST /api/citas trae el paciente dentro del cuerpo.
            foreach (var argumento in actionContext.ActionArguments.Values)
            {
                var cita = argumento as AgendarCitaRequest;
                if (cita != null)
                {
                    return cita.PacienteId;
                }
            }

            return null;
        }
    }
}
