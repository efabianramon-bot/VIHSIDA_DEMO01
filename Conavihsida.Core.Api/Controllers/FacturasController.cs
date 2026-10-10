using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;

namespace Conavihsida.Core.Api.Controllers
{
    [RoutePrefix("api/pacientes/{pacienteId:int}/facturas")]
    [SoloElPaciente]
    public class FacturasController : BaseApiController
    {
        private FacturacionService CrearServicio()
        {
            var repo = new FacturaRepository(ConnectionString);
            return new FacturacionService(repo, CrearAuditoriaService());
        }

        [HttpGet, Route("")]
        public IHttpActionResult Listar(int pacienteId)
        {
            return Ok(CrearServicio().ObtenerFacturasPaciente(pacienteId));
        }

        [HttpGet, Route("{facturaId:int}")]
        public IHttpActionResult Detalle(int pacienteId, int facturaId)
        {
            var factura = CrearServicio().ObtenerFactura(pacienteId, facturaId);
            return factura == null ? (IHttpActionResult)NotFound() : Ok(factura);
        }

        // Nota: cajeroId siempre null aqui, porque este endpoint es para
        // el pago self-service desde el Portal Web (RF-WEB-02). El flujo
        // de Caja (Beckham) con cajero real es un endpoint/servicio aparte.
        [HttpPost, Route("{facturaId:int}/pagar")]
        public IHttpActionResult Pagar(int pacienteId, int facturaId, [FromBody] PagarFacturaRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicio().PagarFactura(
                pacienteId, facturaId, body.FormaPago, body.ReferenciaPasarela,
                cajeroId: null, terminal: ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }
    }
}
