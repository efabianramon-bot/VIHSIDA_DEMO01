using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;

namespace Conavihsida.Core.Api.Controllers
{
    [RoutePrefix("api/pacientes/{pacienteId:int}/resultados")]
    [SoloElPaciente]
    public class ResultadosController : BaseApiController
    {
        [HttpGet, Route("")]
        public IHttpActionResult Listar(int pacienteId)
        {
            // RN-01: ResultadoService lee de vw_ResultadosPaciente, que no
            // tiene ValorResultado. No "arregles" esto agregando ese campo
            // aqui ni en el servicio, aunque algun dia alguien lo pida para
            // "ver mas detalle" -- eso tiene que pasar por el portal medico,
            // no por este endpoint de paciente.
            var repo = new ResultadoRepository(ConnectionString);
            var servicio = new ResultadoService(repo, CrearAuditoriaService());
            return Ok(servicio.ObtenerResultadosPaciente(pacienteId, ObtenerTerminal()));
        }
    }
}
