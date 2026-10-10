using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-01: login de empleados. Respalda POST /api/auth/login, que
    /// llama el Gateway. El CORE solo responde quien es el usuario y que
    /// roles tiene; el token JWT lo genera el Gateway (RF-INT-01).
    /// </summary>
    public class AuthService
    {
        private readonly IUsuarioRepository _usuarios;
        private readonly AuditoriaService _auditoria;

        public AuthService(IUsuarioRepository usuarios, AuditoriaService auditoria)
        {
            _usuarios = usuarios;
            _auditoria = auditoria;
        }

        public ResultadoOperacion<UsuarioAutenticado> Login(string username, string password,
                                                            string origenAcceso, string direccionIp)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                return ResultadoOperacion<UsuarioAutenticado>.Fallo("Usuario y contraseña son obligatorios.");
            }

            // SesionesLog tiene un CHECK sobre el origen: validarlo aqui
            // evita que un valor raro termine en un error de SQL.
            if (!OrigenesAcceso.EsValido(origenAcceso))
            {
                return ResultadoOperacion<UsuarioAutenticado>.Fallo("El origen de acceso no es válido.");
            }

            string mensajeError;
            var usuario = _usuarios.ValidarCredenciales(username.Trim(), password, origenAcceso,
                                                        direccionIp, out mensajeError);
            // El detalle fino de cada intento (usuario tecleado, motivo) ya
            // queda en SesionesLog; aqui va el rastro para la bitacora general.
            if (usuario == null)
            {
                _auditoria.Registrar(null, null, direccionIp, "LoginFallido", "usuarios", null,
                    "origen=" + origenAcceso);
                return ResultadoOperacion<UsuarioAutenticado>.Fallo(mensajeError);
            }

            _auditoria.Registrar(usuario.Id, null, direccionIp, "IniciarSesion", "usuarios", usuario.Id,
                "origen=" + origenAcceso);

            return ResultadoOperacion<UsuarioAutenticado>.Ok(usuario);
        }
    }
}
