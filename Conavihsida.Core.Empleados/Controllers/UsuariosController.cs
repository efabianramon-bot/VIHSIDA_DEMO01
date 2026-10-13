using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    // RF-CORE-01: administracion de usuarios. Las reglas (formato, correo
    // repetido, no desactivarse a uno mismo) son de la API; aqui solo se
    // muestran sus mensajes.
    [AutorizarRol(NombresRol.Administrador)]
    public class UsuariosController : Controller
    {
        private SesionEmpleado Sesion
        {
            get { return EmpleadoPrincipal.SesionActual(HttpContext); }
        }

        private CoreApiClient Api
        {
            get { return new CoreApiClient(Sesion, Request.UserHostAddress); }
        }

        public async Task<ActionResult> Index()
        {
            var respuesta = await Api.GetAsync<List<UsuarioAdminApi>>("api/usuarios");
            return View(new UsuariosInicioViewModel
            {
                Usuarios = respuesta.Datos ?? new List<UsuarioAdminApi>(),
                UsuarioActualId = Sesion.Id,
                Error = TempData["Error"] as string ?? (respuesta.Exitosa ? null : respuesta.Error),
                Exito = TempData["Exito"] as string
            });
        }

        [HttpGet]
        public async Task<ActionResult> Nuevo()
        {
            return View("Formulario", await ArmarFormularioAsync(new UsuarioForm(), null));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Nuevo(UsuarioForm usuario)
        {
            usuario.Id = null;
            var respuesta = await Api.PostAsync<UsuarioAdminApi>("api/usuarios", new
            {
                username = usuario.Username,
                email = usuario.Email,
                nombres = usuario.Nombres,
                apellidos = usuario.Apellidos,
                rol = usuario.Rol,
                filialId = usuario.FilialId,
                password = usuario.Password
            });

            if (!respuesta.Exitosa)
            {
                return View("Formulario", await ArmarFormularioAsync(usuario, respuesta.Error));
            }

            TempData["Exito"] = "Usuario " + respuesta.Datos.Username + " registrado con el rol " + respuesta.Datos.Rol + ".";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Editar(int id)
        {
            var respuesta = await Api.GetAsync<UsuarioAdminApi>("api/usuarios/" + id);
            if (!respuesta.Exitosa || respuesta.Datos == null)
            {
                TempData["Error"] = "No existe el usuario n.º " + id + ".";
                return RedirectToAction("Index");
            }

            var actual = respuesta.Datos;
            return View("Formulario", await ArmarFormularioAsync(new UsuarioForm
            {
                Id = actual.Id,
                Username = actual.Username,
                Email = actual.Email,
                Nombres = actual.Nombres,
                Apellidos = actual.Apellidos,
                Rol = actual.Rol,
                FilialId = actual.FilialId
            }, null));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Editar(int id, UsuarioForm usuario)
        {
            usuario.Id = id;
            var respuesta = await Api.PutAsync<UsuarioAdminApi>("api/usuarios/" + id, new
            {
                email = usuario.Email,
                nombres = usuario.Nombres,
                apellidos = usuario.Apellidos,
                rol = usuario.Rol,
                filialId = usuario.FilialId,
                // Vacia = no cambiarla; el cliente no envia valores null.
                password = string.IsNullOrEmpty(usuario.Password) ? null : usuario.Password
            });

            if (!respuesta.Exitosa)
            {
                return View("Formulario", await ArmarFormularioAsync(usuario, respuesta.Error));
            }

            TempData["Exito"] = "Usuario " + respuesta.Datos.Username + " actualizado"
                + (string.IsNullOrEmpty(usuario.Password) ? "." : ", con contraseña nueva.");
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> CambiarEstado(int id, bool activo)
        {
            var respuesta = await Api.PutAsync<UsuarioAdminApi>("api/usuarios/" + id + "/estado", new { activo });
            if (respuesta.Exitosa)
            {
                TempData["Exito"] = "Cuenta de " + respuesta.Datos.Username + (activo ? " activada." : " desactivada.");
            }
            else
            {
                TempData["Error"] = respuesta.Error;
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Desbloquear(int id)
        {
            var respuesta = await Api.PostAsync<UsuarioAdminApi>("api/usuarios/" + id + "/desbloquear", null);
            if (respuesta.Exitosa)
            {
                TempData["Exito"] = "Cuenta de " + respuesta.Datos.Username + " desbloqueada.";
            }
            else
            {
                TempData["Error"] = respuesta.Error;
            }
            return RedirectToAction("Index");
        }

        // La contraseña nunca se devuelve al formulario, ni siquiera tras un error.
        private async Task<UsuarioFormViewModel> ArmarFormularioAsync(UsuarioForm usuario, string error)
        {
            usuario.Password = null;

            var api = Api;
            var tareaRoles = api.GetAsync<List<string>>("api/usuarios/roles");
            var tareaFiliales = api.GetAsync<List<FilialApi>>("api/usuarios/filiales");
            await Task.WhenAll(tareaRoles, tareaFiliales);

            return new UsuarioFormViewModel
            {
                Usuario = usuario,
                Roles = tareaRoles.Result.Datos ?? new List<string>(),
                // Una filial inactiva solo se ofrece si el usuario ya la tiene.
                Filiales = (tareaFiliales.Result.Datos ?? new List<FilialApi>())
                    .Where(f => f.Activa || f.Id == usuario.FilialId).ToList(),
                Error = error ?? (tareaRoles.Result.Exitosa ? null : tareaRoles.Result.Error)
            };
        }
    }
}
