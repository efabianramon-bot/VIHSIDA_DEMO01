using System;
using System.Linq;
using System.Text.RegularExpressions;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Login de pacientes del Portal Web. Respalda
    /// POST /api/auth/paciente/login, que llama el Gateway. Igual que con
    /// los empleados, el CORE solo dice quien es el paciente; el token lo
    /// genera el Gateway, que despues manda X-Paciente-Id en cada peticion.
    /// </summary>
    public class AuthPacienteService
    {
        public const string MensajeBloqueado =
            "Demasiados intentos fallidos. Intenta de nuevo en unos minutos.";

        public const string MensajeYaRegistrado =
            "No se pudo crear la cuenta: el correo o la cédula ya están registrados.";

        private const int MaximoIntentos = 5;
        private const int MinutosBloqueo = 15;

        // La cedula dominicana tiene 11 digitos (000-0000000-0).
        private const int LargoCedula = 11;
        private const int LargoMaximoNombre = 150;
        private const int LargoMaximoCorreo = 150;
        private const int LargoMinimoPassword = 8;

        // BCrypt ignora lo que pase de 72 bytes.
        private const int LargoMaximoPassword = 72;

        private static readonly Regex FormatoCorreo = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

        private readonly IPacienteRepository _pacientes;
        private readonly AuditoriaService _auditoria;

        public AuthPacienteService(IPacienteRepository pacientes, AuditoriaService auditoria)
        {
            _pacientes = pacientes;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Registro de un paciente desde el Portal Web. Crea solo su cuenta
        /// (identidad civil): el expediente clinico lo abre despues un
        /// medico, y mientras no lo tenga no cuenta con subsidio.
        /// </summary>
        public ResultadoOperacion<PacienteAutenticado> Registrar(
            string nombreCompleto, string cedula, string correo, string password,
            DateTime? fechaNacimiento, string telefono, string terminal)
        {
            // El telefono es opcional y se guarda solo con digitos.
            string digitosTelefono = new string((telefono ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digitosTelefono.Length != 0 && (digitosTelefono.Length < 7 || digitosTelefono.Length > 15))
            {
                return Fallo("El teléfono debe tener entre 7 y 15 dígitos.");
            }
            telefono = digitosTelefono.Length == 0 ? null : digitosTelefono;

            nombreCompleto = (nombreCompleto ?? string.Empty).Trim();
            correo = (correo ?? string.Empty).Trim().ToLowerInvariant();
            string digitos = new string((cedula ?? string.Empty).Where(char.IsDigit).ToArray());

            if (nombreCompleto.Length == 0 || nombreCompleto.Length > LargoMaximoNombre)
            {
                return Fallo("El nombre completo es obligatorio y admite máximo 150 caracteres.");
            }

            if (digitos.Length != LargoCedula)
            {
                return Fallo("La cédula debe tener 11 dígitos.");
            }

            if (correo.Length > LargoMaximoCorreo || !FormatoCorreo.IsMatch(correo))
            {
                return Fallo("El correo electrónico no es válido.");
            }

            if (string.IsNullOrEmpty(password) || password.Length < LargoMinimoPassword
                || System.Text.Encoding.UTF8.GetByteCount(password) > LargoMaximoPassword)
            {
                return Fallo("La contraseña debe tener entre 8 y 72 caracteres.");
            }

            if (fechaNacimiento != null
                && (fechaNacimiento.Value.Date > DateTime.Today || fechaNacimiento.Value.Year < 1900))
            {
                return Fallo("La fecha de nacimiento no es válida.");
            }

            // La cedula se guarda siempre con el formato 000-0000000-0.
            string cedulaConGuiones = digitos.Insert(10, "-").Insert(3, "-");

            var paciente = _pacientes.Registrar(nombreCompleto, cedulaConGuiones, correo, password,
                fechaNacimiento == null ? (DateTime?)null : fechaNacimiento.Value.Date, telefono, DateTime.Now);
            if (paciente == null)
            {
                // Un solo mensaje para los dos casos: no se dice cual de los
                // dos datos ya estaba, para revelar lo menos posible.
                return Fallo(MensajeYaRegistrado);
            }

            _auditoria.Registrar(null, paciente.PacienteId, terminal, "RegistrarPaciente",
                "pacientes", paciente.PacienteId, null);
            return ResultadoOperacion<PacienteAutenticado>.Ok(paciente);
        }

        private static ResultadoOperacion<PacienteAutenticado> Fallo(string mensaje)
        {
            return ResultadoOperacion<PacienteAutenticado>.Fallo(mensaje);
        }

        public ResultadoOperacion<PacienteAutenticado> Login(string correo, string password, string terminal)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrEmpty(password))
            {
                return ResultadoOperacion<PacienteAutenticado>.Fallo("Correo y contraseña son obligatorios.");
            }

            PacienteAutenticado paciente;
            var resultado = _pacientes.ValidarCredenciales(
                correo.Trim(), password, DateTime.Now, MaximoIntentos, MinutosBloqueo, out paciente);

            if (resultado == ResultadoLoginPaciente.Correcto)
            {
                _auditoria.Registrar(null, paciente.PacienteId, terminal, "IniciarSesionPaciente",
                    "pacientes", paciente.PacienteId, null);
                return ResultadoOperacion<PacienteAutenticado>.Ok(paciente);
            }

            // Ojo: en la bitacora NO va el correo tecleado. Y el mensaje es
            // el mismo si el correo no existe o la contraseña esta mal, para
            // no revelar que correos estan registrados.
            _auditoria.Registrar(null, null, terminal, "LoginPacienteFallido", "pacientes", null,
                resultado == ResultadoLoginPaciente.BloqueadoTemporalmente ? "bloqueado" : null);

            return ResultadoOperacion<PacienteAutenticado>.Fallo(
                resultado == ResultadoLoginPaciente.BloqueadoTemporalmente
                    ? MensajeBloqueado
                    : "Credenciales incorrectas.");
        }
    }
}
