using System;
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

        private const int MaximoIntentos = 5;
        private const int MinutosBloqueo = 15;

        private readonly IPacienteRepository _pacientes;
        private readonly AuditoriaService _auditoria;

        public AuthPacienteService(IPacienteRepository pacientes, AuditoriaService auditoria)
        {
            _pacientes = pacientes;
            _auditoria = auditoria;
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
