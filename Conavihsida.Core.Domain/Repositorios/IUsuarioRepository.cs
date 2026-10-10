using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IUsuarioRepository
    {
        /// <summary>
        /// Verifica usuario y contraseña (BCrypt) y deja el intento en
        /// SesionesLog. Devuelve null si las credenciales no sirven; el
        /// motivo que se puede mostrar al usuario queda en mensajeError.
        /// </summary>
        UsuarioAutenticado ValidarCredenciales(string username, string password,
                                               string origenAcceso, string direccionIp,
                                               out string mensajeError);
    }
}
