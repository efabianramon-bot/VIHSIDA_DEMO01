using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

namespace Conavihsida.Core.Api.Controllers
{
    // RN-01: todo lo clinico es solo para el rol Medico. Un Cajero (o
    // cualquier otro rol) recibe 403 antes de tocar la base de datos.
    [RoutePrefix("api/clinica")]
    [RequiereRol(NombresRol.Medico)]
    public class ClinicaController : BaseApiController
    {
        private ExpedienteService CrearServicio()
        {
            var repo = new ExpedienteRepository(ConnectionString);
            return new ExpedienteService(repo, CrearAuditoriaService());
        }

        [HttpGet, Route("expediente/{codigo}")]
        public IHttpActionResult ObtenerExpediente(string codigo)
        {
            var expediente = CrearServicio().ObtenerExpediente(
                codigo, Identidad.UsuarioId.Value, ObtenerTerminal());
            return expediente == null ? (IHttpActionResult)NotFound() : Ok(expediente);
        }

        [HttpPost, Route("expediente")]
        public IHttpActionResult AbrirExpediente([FromBody] AbrirExpedienteRequest body)
        {
            if (body == null || body.PacienteId <= 0)
            {
                return BadRequest("El pacienteId es obligatorio.");
            }

            var resultado = CrearServicio().AbrirExpediente(
                body.PacienteId, body.MedicoResponsableId, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri + "/" + resultado.Valor.CodigoSerologico, resultado.Valor);
        }

        [HttpPut, Route("expediente/{codigo}")]
        public IHttpActionResult ActualizarExpediente(string codigo, [FromBody] ActualizarExpedienteRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicio().ActualizarExpediente(
                codigo, body.MedicoResponsableId, body.Estado, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }
    }
}
