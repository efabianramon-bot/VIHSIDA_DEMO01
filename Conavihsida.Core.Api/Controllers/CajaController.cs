using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

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
    }
}
