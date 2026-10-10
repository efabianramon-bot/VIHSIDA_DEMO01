using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;

namespace Conavihsida.Core.Empleados.Controllers
{
    public class InicioController : Controller
    {
        // Pantalla de entrada: los portales que le corresponden al empleado.
        public ActionResult Index()
        {
            return View(MenuPrincipal.Para(EmpleadoPrincipal.SesionActual(HttpContext)));
        }
    }
}
