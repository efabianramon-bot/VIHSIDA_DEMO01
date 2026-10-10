using System;
using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Domain.Catalogos;

namespace Conavihsida.Core.Api.Controllers
{
    // RF-CORE-05: la bitacora se consulta, nunca se edita ni se borra
    // desde la API. Por eso este controlador solo tiene un GET.
    [RoutePrefix("api/auditoria")]
    [RequiereRol(NombresRol.Auditor, NombresRol.Administrador)]
    public class AuditoriaController : BaseApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Consultar([FromUri] DateTime? desde = null, [FromUri] DateTime? hasta = null,
                                           [FromUri] int? usuarioId = null, [FromUri] string accion = null,
                                           [FromUri] int? maximo = null)
        {
            return Ok(CrearAuditoriaService().Consultar(desde, hasta, usuarioId, accion, maximo));
        }
    }
}
