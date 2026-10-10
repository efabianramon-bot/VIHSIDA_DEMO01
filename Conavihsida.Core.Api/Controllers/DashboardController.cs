using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

namespace Conavihsida.Core.Api.Controllers
{
    // RF-CORE-08. "dias" es el periodo hacia atras (90 por defecto, maximo
    // 365); "diasAviso" es con cuanta anticipacion avisar de un lote por
    // vencer (60 por defecto).
    [RoutePrefix("api/dashboard")]
    [RequiereRol(NombresRol.Administrador)]
    public class DashboardController : BaseApiController
    {
        private DashboardService CrearServicio()
        {
            return new DashboardService(
                new DashboardRepository(ConnectionString), new InventarioRepository(ConnectionString));
        }

        // Los cuatro indicadores en una sola llamada, para pintar el tablero.
        [HttpGet, Route("")]
        public IHttpActionResult Resumen([FromUri] int? dias = null, [FromUri] int? diasAviso = null)
        {
            return Ok(CrearServicio().ObtenerResumen(dias, diasAviso));
        }

        [HttpGet, Route("pacientes-activos")]
        public IHttpActionResult PacientesActivos([FromUri] int? dias = null)
        {
            return Ok(CrearServicio().ObtenerPacientesActivos(dias));
        }

        [HttpGet, Route("adherencia")]
        public IHttpActionResult Adherencia([FromUri] int? dias = null)
        {
            return Ok(CrearServicio().ObtenerAdherencia(dias));
        }

        [HttpGet, Route("inventario-critico")]
        public IHttpActionResult InventarioCritico([FromUri] int? diasAviso = null)
        {
            return Ok(CrearServicio().ObtenerInventarioCritico(diasAviso));
        }

        [HttpGet, Route("subsidios")]
        public IHttpActionResult Subsidios([FromUri] int? dias = null)
        {
            return Ok(CrearServicio().ObtenerSubsidios(dias));
        }
    }
}
