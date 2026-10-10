using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    // RF-CORE-08: Dashboard Ejecutivo. La API solo entrega totales: aqui
    // no hay nombres, cedulas, codigos de expediente ni valores clinicos.
    [AutorizarRol(NombresRol.Administrador)]
    public class DashboardController : Controller
    {
        private const int DiasPorDefecto = 90;

        public async Task<ActionResult> Index(int? dias)
        {
            // Solo los periodos que ofrece la pantalla; cualquier otro
            // valor en la URL cae al periodo por defecto.
            int periodo = dias != null && DashboardViewModel.Periodos.Contains(dias.Value)
                ? dias.Value : DiasPorDefecto;

            var api = new CoreApiClient(EmpleadoPrincipal.SesionActual(HttpContext), Request.UserHostAddress);
            var respuesta = await api.GetAsync<DashboardApi>("api/dashboard?dias=" + periodo);

            return View(new DashboardViewModel
            {
                Datos = respuesta.Datos,
                Dias = periodo,
                Error = respuesta.Exitosa ? null : respuesta.Error
            });
        }
    }
}
