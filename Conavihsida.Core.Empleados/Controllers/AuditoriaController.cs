using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    // RF-CORE-05: la bitacora solo se consulta. No hay forma de editarla
    // ni borrarla, ni aqui ni en la API.
    [AutorizarRol(NombresRol.Auditor, NombresRol.Administrador)]
    public class AuditoriaController : Controller
    {
        private const int MaximoPorDefecto = 100;

        public async Task<ActionResult> Index(DateTime? desde, DateTime? hasta, int? usuarioId,
                                              string accion, int? maximo)
        {
            int limite = maximo != null && BitacoraViewModel.Limites.Contains(maximo.Value)
                ? maximo.Value : MaximoPorDefecto;
            accion = string.IsNullOrWhiteSpace(accion) ? null : accion.Trim();

            var filtros = new List<string> { "maximo=" + limite };
            if (desde != null)
            {
                filtros.Add("desde=" + desde.Value.ToString("yyyy-MM-dd"));
            }
            if (hasta != null)
            {
                filtros.Add("hasta=" + hasta.Value.ToString("yyyy-MM-dd"));
            }
            if (usuarioId != null)
            {
                filtros.Add("usuarioId=" + usuarioId.Value);
            }
            if (accion != null)
            {
                filtros.Add("accion=" + Uri.EscapeDataString(accion));
            }

            var api = new CoreApiClient(EmpleadoPrincipal.SesionActual(HttpContext), Request.UserHostAddress);
            var respuesta = await api.GetAsync<List<RegistroAuditoriaApi>>("api/auditoria?" + string.Join("&", filtros));

            return View(new BitacoraViewModel
            {
                Registros = respuesta.Datos ?? new List<RegistroAuditoriaApi>(),
                Desde = desde,
                Hasta = hasta,
                UsuarioId = usuarioId,
                Accion = accion,
                Maximo = limite,
                Error = respuesta.Exitosa ? null : respuesta.Error
            });
        }
    }
}
