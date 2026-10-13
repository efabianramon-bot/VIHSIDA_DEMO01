namespace Conavihsida.Core.Api.Dtos
{
    /// <summary>Cuerpo de POST /api/auth/login.</summary>
    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }

        /// <summary>Desde donde entra el usuario: WEB, CAJA o CORE.</summary>
        public string Origen { get; set; }
    }

    /// <summary>Cuerpo de POST /api/auth/paciente/registro.</summary>
    public class RegistrarPacienteRequest
    {
        public string NombreCompleto { get; set; }

        /// <summary>11 digitos, con o sin guiones.</summary>
        public string Cedula { get; set; }
        public string Correo { get; set; }
        public string Password { get; set; }

        /// <summary>Opcional.</summary>
        public System.DateTime? FechaNacimiento { get; set; }
    }

    /// <summary>Cuerpo de POST /api/auth/paciente/login.</summary>
    public class LoginPacienteRequest
    {
        public string Correo { get; set; }
        public string Password { get; set; }
    }
}
