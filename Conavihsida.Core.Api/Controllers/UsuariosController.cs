using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Api.Controllers
{
    // RF-CORE-01: gestion de empleados. Solo el Administrador. Ninguna
    // respuesta lleva la contraseña ni su hash.
    [RoutePrefix("api/usuarios")]
    [RequiereRol(NombresRol.Administrador)]
    public class UsuariosController : BaseApiController
    {
        private AdministracionUsuariosService CrearServicio()
        {
            return new AdministracionUsuariosService(
                new AdministracionUsuariosRepository(ConnectionString), CrearAuditoriaService());
        }

        [HttpGet, Route("")]
        public IHttpActionResult Listar()
        {
            return Ok(CrearServicio().Listar());
        }

        [HttpGet, Route("{usuarioId:int}")]
        public IHttpActionResult Obtener(int usuarioId)
        {
            var usuario = CrearServicio().Obtener(usuarioId);
            return usuario == null ? (IHttpActionResult)NotFound() : Ok(usuario);
        }

        // Catalogos para armar el formulario.
        [HttpGet, Route("filiales")]
        public IHttpActionResult Filiales()
        {
            return Ok(CrearServicio().ListarFiliales());
        }

        [HttpGet, Route("roles")]
        public IHttpActionResult Roles()
        {
            return Ok(NombresRol.Todos);
        }

        [HttpPost, Route("")]
        public IHttpActionResult Crear([FromBody] CrearUsuarioRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var resultado = CrearServicio().Crear(
                body.Username, body.Email, body.Nombres, body.Apellidos, body.Rol, body.FilialId,
                body.Password, Identidad.UsuarioId.Value, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri + "/" + resultado.Valor.Id, resultado.Valor);
        }

        [HttpPut, Route("{usuarioId:int}")]
        public IHttpActionResult Actualizar(int usuarioId, [FromBody] ActualizarUsuarioRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            return Responder(CrearServicio().Actualizar(
                usuarioId, body.Email, body.Nombres, body.Apellidos, body.Rol, body.FilialId,
                body.Password, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        [HttpPut, Route("{usuarioId:int}/estado")]
        public IHttpActionResult CambiarEstado(int usuarioId, [FromBody] CambiarEstadoUsuarioRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            return Responder(CrearServicio().CambiarEstado(
                usuarioId, body.Activo, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        // Quita el bloqueo por intentos fallidos de inicio de sesion.
        [HttpPost, Route("{usuarioId:int}/desbloquear")]
        public IHttpActionResult Desbloquear(int usuarioId)
        {
            return Responder(CrearServicio().Desbloquear(usuarioId, Identidad.UsuarioId.Value, ObtenerTerminal()));
        }

        private IHttpActionResult Responder(ResultadoOperacion<UsuarioAdministrado> resultado)
        {
            if (resultado.Exitoso)
            {
                return Ok(resultado.Valor);
            }

            if (resultado.MensajeError == AdministracionUsuariosService.MensajeNoExiste)
            {
                return NotFound();
            }

            return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
        }
    }
}
