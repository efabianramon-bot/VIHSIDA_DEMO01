using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;

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

        [HttpGet, Route("expedientes")]
        public IHttpActionResult ListarExpedientes()
        {
            return Ok(CrearServicio().ListarExpedientes(Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        // Unico punto donde se cruza la identidad civil con el codigo del
        // expediente; por eso es solo para Medico y siempre se audita.
        [HttpGet, Route("pacientes")]
        public IHttpActionResult BuscarPaciente([FromUri] string cedula = null, [FromUri] int? pacienteId = null)
        {
            if (string.IsNullOrWhiteSpace(cedula) && pacienteId == null)
            {
                return BadRequest("Indica la cedula o el pacienteId del paciente.");
            }

            var paciente = CrearServicio().BuscarPaciente(
                cedula, pacienteId, Identidad.UsuarioId.Value, ObtenerTerminal());
            return paciente == null ? (IHttpActionResult)NotFound() : Ok(paciente);
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

        [HttpGet, Route("expediente/{codigo}/historial")]
        public IHttpActionResult ObtenerHistorial(string codigo)
        {
            VencerRecetasAntiguas();
            var consultas = CrearServicioHistorial().ObtenerHistorial(
                codigo, Identidad.UsuarioId.Value, ObtenerTerminal());
            return consultas == null ? (IHttpActionResult)NotFound() : Ok(consultas);
        }

        // El medico de la consulta es siempre quien llama (X-Usuario-Id):
        // no se acepta en el cuerpo para que nadie firme por otro.
        [HttpPost, Route("expediente/{codigo}/consultas")]
        public IHttpActionResult RegistrarConsulta(string codigo, [FromBody] RegistrarConsultaRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var recetas = (body.Recetas ?? new List<RecetaRequest>())
                .Where(r => r != null)
                .Select(r => new Receta
                {
                    MedicamentoId = r.MedicamentoId,
                    EsquemaTarv = r.EsquemaTarv,
                    CantidadPrescrita = r.CantidadPrescrita
                })
                .ToList();

            var resultado = CrearServicioHistorial().RegistrarConsulta(
                codigo, Identidad.UsuarioId.Value, body.CargaViral, body.ConteoCd4,
                body.DiagnosticoDetalle, body.NotasClinicas, recetas, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                if (resultado.MensajeError == HistorialService.MensajeExpedienteNoExiste)
                {
                    return NotFound();
                }
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri.ToString(), resultado.Valor);
        }

        // Resultados de laboratorio, lado clinico: aqui si viaja el valor.
        // El paciente los ve por /api/pacientes/{id}/resultados, sin valor.
        [HttpGet, Route("expediente/{codigo}/resultados")]
        public IHttpActionResult ObtenerResultados(string codigo)
        {
            var resultados = CrearServicioLaboratorio().ObtenerResultados(
                codigo, Identidad.UsuarioId.Value, ObtenerTerminal());
            return resultados == null ? (IHttpActionResult)NotFound() : Ok(resultados);
        }

        [HttpPost, Route("expediente/{codigo}/resultados")]
        public IHttpActionResult SolicitarPrueba(string codigo, [FromBody] SolicitarPruebaRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicioLaboratorio().SolicitarPrueba(
                codigo, body.Prueba, body.NotaPaciente, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                if (resultado.MensajeError == LaboratorioService.MensajeExpedienteNoExiste)
                {
                    return NotFound();
                }
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri.ToString(), resultado.Valor);
        }

        [HttpPut, Route("resultados/{resultadoId:int}")]
        public IHttpActionResult PublicarResultado(int resultadoId, [FromBody] PublicarResultadoRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicioLaboratorio().PublicarResultado(
                resultadoId, body.ValorResultado, body.NotaPaciente, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                if (resultado.MensajeError == LaboratorioService.MensajeResultadoNoExiste)
                {
                    return NotFound();
                }
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }

        private LaboratorioService CrearServicioLaboratorio()
        {
            return new LaboratorioService(
                new ResultadoRepository(ConnectionString),
                new ExpedienteRepository(ConnectionString),
                CrearAuditoriaService());
        }

        private HistorialService CrearServicioHistorial()
        {
            return new HistorialService(
                new HistorialRepository(ConnectionString),
                new ExpedienteRepository(ConnectionString),
                CrearAuditoriaService());
        }
    }
}
