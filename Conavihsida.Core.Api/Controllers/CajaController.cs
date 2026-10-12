using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Api.Controllers
{
    // Lo que el modulo de Caja le pide al CORE a traves de Integracion.
    // Aqui nunca viaja informacion clinica ni el codigo serologico (RN-01).
    [RoutePrefix("api/caja")]
    [RequiereRol(NombresRol.Cajero, NombresRol.Administrador)]
    public class CajaController : BaseApiController
    {
        // La cedula va en la consulta (?cedula=) y no en la ruta, para que
        // no quede escrita en el log de transacciones.
        [HttpGet, Route("subsidio")]
        public IHttpActionResult ConsultarSubsidio([FromUri] string cedula = null)
        {
            var servicio = new SubsidioService(new ExpedienteRepository(ConnectionString), CrearAuditoriaService());
            var subsidio = servicio.ConsultarPorCedula(cedula, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (subsidio == null)
            {
                return BadRequest("Indica la cedula del paciente (11 digitos, con o sin guiones) en ?cedula=.");
            }

            return Ok(subsidio);
        }

        // ----- Sincronizacion: lo que cada terminal de Caja guardo sin
        // conexion. El cuerpo y la respuesta { exito, mensaje } son los del
        // contrato de Caja, para que Integracion pueda reenviarlos tal cual.
        // Reenviar un registro ya recibido lo actualiza y responde 200. -----

        [HttpPost, Route("sync/turnos")]
        public IHttpActionResult SincronizarTurno([FromBody] TurnoCaja body)
        {
            return body == null ? SinCuerpo()
                : Responder(CrearServicioSync().RecibirTurno(body, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        [HttpPost, Route("sync/movimientos")]
        public IHttpActionResult SincronizarMovimiento([FromBody] MovimientoCaja body)
        {
            return body == null ? SinCuerpo()
                : Responder(CrearServicioSync().RecibirMovimiento(body, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        [HttpPost, Route("sync/transacciones")]
        public IHttpActionResult SincronizarTransaccion([FromBody] TransaccionCaja body)
        {
            return body == null ? SinCuerpo()
                : Responder(CrearServicioSync().RecibirTransaccion(body, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        [HttpPost, Route("sync/comprobantes")]
        public IHttpActionResult SincronizarComprobante([FromBody] ComprobanteCaja body)
        {
            return body == null ? SinCuerpo()
                : Responder(CrearServicioSync().RecibirComprobante(body, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        [HttpPost, Route("sync/fondos")]
        public IHttpActionResult SincronizarFondo([FromBody] FondoConvenio body)
        {
            return body == null ? SinCuerpo()
                : Responder(CrearServicioSync().RecibirFondo(body, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        private SincronizacionCajaService CrearServicioSync()
        {
            return new SincronizacionCajaService(new CajaRepository(ConnectionString), CrearAuditoriaService());
        }

        // Un dato invalido responde 400: Caja deja el registro pendiente y
        // lo reintenta, asi que el mensaje dice que campo corregir. "error"
        // repite el mensaje para el log de transacciones.
        private IHttpActionResult Responder(ResultadoOperacion<bool> resultado)
        {
            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.BadRequest,
                    new { exito = false, mensaje = resultado.MensajeError, error = resultado.MensajeError });
            }

            return Ok(new
            {
                exito = true,
                mensaje = resultado.Valor ? "Registro recibido." : "Registro actualizado."
            });
        }

        private IHttpActionResult SinCuerpo()
        {
            const string mensaje = "El cuerpo de la peticion es obligatorio o no es un JSON valido.";
            return Content(HttpStatusCode.BadRequest, new { exito = false, mensaje, error = mensaje });
        }
    }
}
