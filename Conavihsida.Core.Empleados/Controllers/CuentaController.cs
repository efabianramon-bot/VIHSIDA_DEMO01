using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    public class CuentaController : Controller
    {
        // Asi lo registra la API en SesionesLog: entro desde la aplicacion de empleados.
        private const string OrigenAcceso = "CORE";

        [AllowAnonymous, HttpGet]
        public ActionResult Login(string returnUrl)
        {
            if (EmpleadoPrincipal.SesionActual(HttpContext) != null)
            {
                return RedirectToAction("Index", "Inicio");
            }
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel modelo)
        {
            if (string.IsNullOrWhiteSpace(modelo.Usuario) || string.IsNullOrEmpty(modelo.Clave))
            {
                return VolverConError(modelo, "Escribe tu usuario y tu contraseña.");
            }

            // La contraseña solo pasa de largo hacia la API, que es quien la
            // valida con BCrypt; aqui no se guarda ni se registra.
            var api = new CoreApiClient(null, Request.UserHostAddress);
            var respuesta = await api.PostAsync<UsuarioApi>("api/auth/login", new
            {
                username = modelo.Usuario.Trim(),
                password = modelo.Clave,
                origen = OrigenAcceso
            });

            if (!respuesta.Exitosa || respuesta.Datos == null)
            {
                return VolverConError(modelo, respuesta.Error ?? "No se pudo iniciar sesión.");
            }

            var usuario = respuesta.Datos;
            SesionEmpleado.Iniciar(Response, new SesionEmpleado
            {
                Id = usuario.Id,
                Username = usuario.Username,
                NombreCompleto = (usuario.Nombres + " " + usuario.Apellidos).Trim(),
                Roles = usuario.Roles
            });

            // Solo se vuelve a rutas de esta misma aplicacion: un returnUrl
            // externo seria una puerta para mandar al empleado a otro sitio.
            if (!string.IsNullOrEmpty(modelo.ReturnUrl) && Url.IsLocalUrl(modelo.ReturnUrl))
            {
                return Redirect(modelo.ReturnUrl);
            }
            return RedirectToAction("Index", "Inicio");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Salir()
        {
            SesionEmpleado.Cerrar();
            return RedirectToAction("Login");
        }

        private ActionResult VolverConError(LoginViewModel modelo, string error)
        {
            // La contraseña nunca se devuelve al formulario.
            ModelState.Clear();
            return View("Login", new LoginViewModel
            {
                Usuario = modelo.Usuario,
                ReturnUrl = modelo.ReturnUrl,
                Error = error
            });
        }
    }
}
