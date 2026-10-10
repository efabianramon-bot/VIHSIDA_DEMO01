using System.Net;
using System.Web.Http;
using Conavihsida.Core.Api.Dtos;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

namespace Conavihsida.Core.Api.Controllers
{
    [RoutePrefix("api/auth")]
    public class AuthController : BaseApiController
    {
        // Este endpoint NO devuelve un token: responde quien es el usuario
        // y sus roles, y el Gateway arma el JWT con eso (RF-INT-01).
        [HttpPost, Route("login")]
        public IHttpActionResult Login([FromBody] LoginRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(body.Username) || string.IsNullOrEmpty(body.Password))
            {
                return BadRequest("Usuario y contraseña son obligatorios.");
            }

            if (!OrigenesAcceso.EsValido(body.Origen))
            {
                return BadRequest("El origen debe ser WEB, CAJA o CORE.");
            }

            var servicio = new AuthService(new UsuarioRepository(ConnectionString));
            var resultado = servicio.Login(body.Username, body.Password, body.Origen, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Unauthorized, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }

        // Devuelve la identidad tal como la recibio el CORE. Sirve para
        // comprobar que el Gateway esta mandando bien los encabezados.
        [HttpGet, Route("yo"), RequiereRol]
        public IHttpActionResult Yo()
        {
            var identidad = Identidad;
            return Ok(new { usuarioId = identidad.UsuarioId, roles = identidad.Roles });
        }
    }
}
