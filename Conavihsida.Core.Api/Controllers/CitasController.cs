using System;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;

namespace Conavihsida.Core.Api.Controllers
{
    [RoutePrefix("api")]
    public class CitasController : BaseApiController
    {
        private CitaService CrearServicio()
        {
            var repo = new CitaRepository(ConnectionString);
            return new CitaService(repo, CrearAuditoriaService());
        }

        [HttpGet, Route("medicos")]
        public IHttpActionResult ListarMedicos()
        {
            return Ok(CrearServicio().ObtenerMedicos());
        }

        [HttpGet, Route("medicos/{medicoId:int}/horarios")]
        public IHttpActionResult HorariosDisponibles(int medicoId, [FromUri] DateTime fecha)
        {
            return Ok(CrearServicio().ObtenerHorariosDisponibles(medicoId, fecha));
        }

        [HttpGet, Route("pacientes/{pacienteId:int}/citas")]
        public IHttpActionResult CitasDelPaciente(int pacienteId)
        {
            return Ok(CrearServicio().ObtenerCitasPaciente(pacienteId));
        }

        [HttpPost, Route("citas")]
        public IHttpActionResult Agendar([FromBody] AgendarCitaRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicio().AgendarCita(
                body.PacienteId, body.MedicoId, body.Fecha, body.Hora, body.Motivo, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri + "/" + resultado.Valor.Id, resultado.Valor);
        }

        [HttpPost, Route("citas/{citaId:int}/cancelar")]
        public IHttpActionResult Cancelar(int citaId, [FromUri] int pacienteId)
        {
            bool cancelada = CrearServicio().CancelarCita(pacienteId, citaId, ObtenerTerminal());
            if (!cancelada)
            {
                return NotFound();
            }
            return Ok();
        }
    }
}
