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

            var servicio = new AuthService(new UsuarioRepository(ConnectionString), CrearAuditoriaService());
            var resultado = servicio.Login(body.Username, body.Password, body.Origen, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Unauthorized, new { error = resultado.MensajeError });
            }

            return Ok(resultado.Valor);
        }

        // Registro de un paciente desde el Portal Web. Crea solo la cuenta;
        // el expediente clinico lo abre despues un medico. 409 si los datos
        // no son validos o el correo o la cedula ya estan registrados.
        [HttpPost, Route("paciente/registro")]
        public IHttpActionResult RegistrarPaciente([FromBody] RegistrarPacienteRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            var servicio = new AuthPacienteService(new PacienteRepository(ConnectionString), CrearAuditoriaService());
            var resultado = servicio.Registrar(body.NombreParaGuardar, body.Cedula, body.Correo,
                body.Password ?? body.Clave, body.FechaNacimiento, body.Telefono, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                return Content(HttpStatusCode.Conflict, new { error = resultado.MensajeError });
            }

            return Created(Request.RequestUri.ToString(), resultado.Valor);
        }

        // Login de pacientes del Portal Web. Tampoco devuelve un token: el
        // Gateway lo arma con el pacienteId y despues lo manda en
        // X-Paciente-Id. 401 si las credenciales no sirven y 429 si la
        // cuenta esta bloqueada por intentos fallidos.
        [HttpPost, Route("paciente/login")]
        public IHttpActionResult LoginPaciente([FromBody] LoginPacienteRequest body)
        {
            if (body == null)
            {
                return BadRequest("El cuerpo de la peticion es obligatorio.");
            }

            // Se acepta "password" o "clave", que es como lo manda el Portal Web.
            string password = body.Password ?? body.Clave;
            if (string.IsNullOrWhiteSpace(body.Correo) || string.IsNullOrEmpty(password))
            {
                return BadRequest("Correo y contraseña son obligatorios.");
            }

            var servicio = new AuthPacienteService(new PacienteRepository(ConnectionString), CrearAuditoriaService());
            var resultado = servicio.Login(body.Correo, password, ObtenerTerminal());

            if (!resultado.Exitoso)
            {
                var codigo = resultado.MensajeError == AuthPacienteService.MensajeBloqueado
                    ? (HttpStatusCode)429
                    : HttpStatusCode.Unauthorized;
                return Content(codigo, new { error = resultado.MensajeError });
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
