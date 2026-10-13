using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Gestion de empleados: alta, consulta, cambios, activar/desactivar y
    /// desbloquear. Respalda /api/usuarios, de uso exclusivo del rol
    /// Administrador (RF-CORE-01).
    /// </summary>
    public class AdministracionUsuariosService
    {
        public const string MensajeNoExiste = "El usuario no existe.";

        private const int LargoMinimoPassword = 8;

        // BCrypt ignora lo que pase de 72 bytes: mejor rechazarlo que
        // aceptar una contraseña de la que solo cuenta una parte.
        private const int LargoMaximoPassword = 72;

        private static readonly Regex FormatoUsername = new Regex(@"^[a-z0-9][a-z0-9._-]{2,49}$");
        private static readonly Regex FormatoEmail = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private readonly IAdministracionUsuariosRepository _usuarios;
        private readonly AuditoriaService _auditoria;

        public AdministracionUsuariosService(IAdministracionUsuariosRepository usuarios, AuditoriaService auditoria)
        {
            _usuarios = usuarios;
            _auditoria = auditoria;
        }

        public List<UsuarioAdministrado> Listar()
        {
            return _usuarios.Listar();
        }

        public UsuarioAdministrado Obtener(int usuarioId)
        {
            return _usuarios.Obtener(usuarioId);
        }

        public List<Filial> ListarFiliales()
        {
            return _usuarios.ListarFiliales();
        }

        public ResultadoOperacion<UsuarioAdministrado> Crear(
            string username, string email, string nombres, string apellidos, string rol, int? filialId,
            string password, int administradorId, string terminal)
        {
            username = (username ?? string.Empty).Trim().ToLowerInvariant();
            if (!FormatoUsername.IsMatch(username))
            {
                return Fallo("El nombre de usuario debe tener de 3 a 50 caracteres: letras minúsculas, números, punto, guion o guion bajo.");
            }

            string error = ValidarDatos(ref email, ref nombres, ref apellidos, rol) ?? ValidarPassword(password);
            if (error != null)
            {
                return Fallo(error);
            }

            var usuario = new UsuarioAdministrado
            {
                Username = username,
                Email = email,
                Nombres = nombres,
                Apellidos = apellidos,
                Rol = rol,
                FilialId = filialId
            };

            var resultado = _usuarios.Crear(usuario, password, DateTime.Now);
            if (resultado != ResultadoGuardarUsuario.Guardado)
            {
                return Fallo(Mensaje(resultado));
            }

            // Ojo: en la bitacora nunca va la contraseña.
            Auditar(administradorId, terminal, "CrearUsuario", usuario.Id,
                string.Format("rol={0};filial={1}", rol, Texto(filialId)));
            return ResultadoOperacion<UsuarioAdministrado>.Ok(_usuarios.Obtener(usuario.Id));
        }

        /// <param name="password">null o vacia = no se cambia la contraseña.</param>
        public ResultadoOperacion<UsuarioAdministrado> Actualizar(
            int usuarioId, string email, string nombres, string apellidos, string rol, int? filialId,
            string password, int administradorId, string terminal)
        {
            password = string.IsNullOrEmpty(password) ? null : password;

            string error = ValidarDatos(ref email, ref nombres, ref apellidos, rol)
                ?? (password == null ? null : ValidarPassword(password));
            if (error != null)
            {
                return Fallo(error);
            }

            var actual = _usuarios.Obtener(usuarioId);
            if (actual == null)
            {
                return Fallo(MensajeNoExiste);
            }

            // Un administrador que se quita su propio rol puede dejar el
            // sistema sin nadie que administre.
            if (usuarioId == administradorId && rol != NombresRol.Administrador)
            {
                return Fallo("No puedes quitarte a ti mismo el rol de Administrador.");
            }

            var usuario = new UsuarioAdministrado
            {
                Id = usuarioId,
                Email = email,
                Nombres = nombres,
                Apellidos = apellidos,
                Rol = rol,
                FilialId = filialId
            };

            var resultado = _usuarios.Actualizar(usuario, password, DateTime.Now);
            if (resultado != ResultadoGuardarUsuario.Guardado)
            {
                return Fallo(Mensaje(resultado));
            }

            Auditar(administradorId, terminal, "ActualizarUsuario", usuarioId,
                string.Format("rol={0};filial={1}", rol, Texto(filialId)));
            if (password != null)
            {
                Auditar(administradorId, terminal, "CambiarClaveUsuario", usuarioId, null);
            }

            return ResultadoOperacion<UsuarioAdministrado>.Ok(_usuarios.Obtener(usuarioId));
        }

        public ResultadoOperacion<UsuarioAdministrado> CambiarEstado(int usuarioId, bool activo,
                                                                    int administradorId, string terminal)
        {
            if (usuarioId == administradorId && !activo)
            {
                return Fallo("No puedes desactivar tu propia cuenta.");
            }

            if (!_usuarios.CambiarEstado(usuarioId, activo, DateTime.Now))
            {
                return Fallo(MensajeNoExiste);
            }

            Auditar(administradorId, terminal, activo ? "ActivarUsuario" : "DesactivarUsuario", usuarioId, null);
            return ResultadoOperacion<UsuarioAdministrado>.Ok(_usuarios.Obtener(usuarioId));
        }

        public ResultadoOperacion<UsuarioAdministrado> Desbloquear(int usuarioId, int administradorId, string terminal)
        {
            if (!_usuarios.Desbloquear(usuarioId, DateTime.Now))
            {
                return Fallo(MensajeNoExiste);
            }

            Auditar(administradorId, terminal, "DesbloquearUsuario", usuarioId, null);
            return ResultadoOperacion<UsuarioAdministrado>.Ok(_usuarios.Obtener(usuarioId));
        }

        /// <summary>Limpia los textos y devuelve el mensaje de error, o null si todo es valido.</summary>
        private static string ValidarDatos(ref string email, ref string nombres, ref string apellidos, string rol)
        {
            email = (email ?? string.Empty).Trim().ToLowerInvariant();
            nombres = (nombres ?? string.Empty).Trim();
            apellidos = (apellidos ?? string.Empty).Trim();

            if (nombres.Length == 0 || nombres.Length > 100 || apellidos.Length == 0 || apellidos.Length > 100)
            {
                return "Nombres y apellidos son obligatorios y admiten máximo 100 caracteres cada uno.";
            }

            if (email.Length > 100 || !FormatoEmail.IsMatch(email))
            {
                return "El correo electrónico no es válido.";
            }

            if (!NombresRol.Todos.Contains(rol))
            {
                return "El rol debe ser uno de: " + string.Join(", ", NombresRol.Todos) + ".";
            }

            return null;
        }

        private static string ValidarPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < LargoMinimoPassword
                || System.Text.Encoding.UTF8.GetByteCount(password) > LargoMaximoPassword)
            {
                return "La contraseña debe tener entre 8 y 72 caracteres.";
            }
            return null;
        }

        private static string Mensaje(ResultadoGuardarUsuario resultado)
        {
            switch (resultado)
            {
                case ResultadoGuardarUsuario.NoExiste:
                    return MensajeNoExiste;
                case ResultadoGuardarUsuario.UsernameRepetido:
                    return "Ya existe un usuario con ese nombre de usuario.";
                case ResultadoGuardarUsuario.EmailRepetido:
                    return "Ya existe un usuario con ese correo electrónico.";
                default:
                    return "El rol o la filial indicados no existen.";
            }
        }

        private void Auditar(int administradorId, string terminal, string accion, int usuarioId, string detalle)
        {
            _auditoria.Registrar(administradorId, null, terminal, accion, "usuarios", usuarioId, detalle);
        }

        private static string Texto(int? valor)
        {
            return valor == null ? "ninguna" : valor.Value.ToString();
        }

        private static ResultadoOperacion<UsuarioAdministrado> Fallo(string mensaje)
        {
            return ResultadoOperacion<UsuarioAdministrado>.Fallo(mensaje);
        }
    }
}
