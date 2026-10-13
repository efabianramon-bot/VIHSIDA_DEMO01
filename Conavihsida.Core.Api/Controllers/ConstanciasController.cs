using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Api.Controllers
{
    // Constancias de lo que el paciente hizo en el Portal Web. El paciente
    // de la ruta manda: el pacienteId, el numero, la fecha y el nombre que
    // vengan en el cuerpo se ignoran.
    [RoutePrefix("api/pacientes/{pacienteId:int}/constancias")]
    [SoloElPaciente]
    public class ConstanciasController : BaseApiController
    {
        private ConstanciaService CrearServicio()
        {
            return new ConstanciaService(new ConstanciaRepository(ConnectionString), CrearAuditoriaService());
        }

        [HttpGet, Route("")]
        public IHttpActionResult Listar(int pacienteId)
        {
            return Ok(CrearServicio().ObtenerPorPaciente(pacienteId));
        }

        [HttpGet, Route("{constanciaId:int}")]
        public IHttpActionResult Obtener(int pacienteId, int constanciaId)
        {
            var constancia = CrearServicio().Obtener(pacienteId, constanciaId);
            return constancia == null ? (IHttpActionResult)NotFound() : Ok(constancia);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Registrar(int pacienteId, [FromBody] ConstanciaPaciente body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio o no es un JSON valido.");
            }

            var resultado = CrearServicio().Registrar(pacienteId, body, ObtenerTerminal());
            if (!resultado.Exitoso)
            {
                if (resultado.MensajeError == ConstanciaService.MensajePacienteNoExiste)
                {
                    return NotFound();
                }
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri + "/" + resultado.Valor.Id, resultado.Valor);
        }
    }
}
