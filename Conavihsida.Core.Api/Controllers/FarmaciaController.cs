using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

namespace Conavihsida.Core.Api.Controllers
{
    // RF-CORE-04 / RF-CORE-07. Aqui nunca viaja informacion clinica: la
    // receta solo trae medicamento, cantidad y codigo serologico (RN-01).
    [RoutePrefix("api/farmacia")]
    public class FarmaciaController : BaseApiController
    {
        private InventarioService CrearServicio()
        {
            return new InventarioService(new InventarioRepository(ConnectionString), CrearAuditoriaService());
        }

        // El medico tambien lo necesita para saber que puede recetar.
        [HttpGet, Route("medicamentos")]
        [RequiereRol(NombresRol.Farmaceutico, NombresRol.Medico, NombresRol.Administrador)]
        public IHttpActionResult ListarMedicamentos()
        {
            return Ok(CrearServicio().ObtenerMedicamentos());
        }

        [HttpGet, Route("lotes")]
        [RequiereRol(NombresRol.Farmaceutico, NombresRol.Administrador)]
        public IHttpActionResult ListarLotes([FromUri] int? medicamentoId = null)
        {
            return Ok(CrearServicio().ObtenerLotes(medicamentoId));
        }

        [HttpPost, Route("lotes")]
        [RequiereRol(NombresRol.Farmaceutico)]
        public IHttpActionResult RegistrarLote([FromBody] RegistrarLoteRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicio().RegistrarLote(
                body.MedicamentoId, body.NumeroLote, body.FechaCaducidad, body.Cantidad,
                body.ExistenciaMinima, body.ExistenciaMaxima, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri + "/" + resultado.Valor.Id, resultado.Valor);
        }

        [HttpGet, Route("recetas/{recetaId:int}")]
        [RequiereRol(NombresRol.Farmaceutico)]
        public IHttpActionResult ObtenerReceta(int recetaId)
        {
            var receta = CrearServicio().ObtenerReceta(recetaId, Identidad.UsuarioId.Value, ObtenerTerminal());
            return receta == null ? (IHttpActionResult)NotFound() : Ok(receta);
        }

        [HttpPost, Route("recetas/{recetaId:int}/despachar")]
        [RequiereRol(NombresRol.Farmaceutico)]
        public IHttpActionResult DespacharReceta(int recetaId)
        {
            var resultado = CrearServicio().DespacharReceta(
                recetaId, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                if (resultado.MensajeError == InventarioService.MensajeRecetaNoExiste)
                {
                    return NotFound();
                }
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }
    }
}
