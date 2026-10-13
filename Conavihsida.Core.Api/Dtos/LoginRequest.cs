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

        // El Portal Web manda el nombre en dos campos; se aceptan las dos formas.
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        /// <summary>11 digitos, con o sin guiones.</summary>
        public string Cedula { get; set; }
        public string Correo { get; set; }
        public string Password { get; set; }

        /// <summary>Mismo dato que Password, con el nombre que usa el Portal Web.</summary>
        public string Clave { get; set; }

        /// <summary>Opcional.</summary>
        public System.DateTime? FechaNacimiento { get; set; }

        /// <summary>Opcional. De 7 a 15 digitos; se guarda solo con digitos.</summary>
        public string Telefono { get; set; }

        public string NombreParaGuardar
        {
            get
            {
                return string.IsNullOrWhiteSpace(NombreCompleto)
                    ? ((Nombres ?? string.Empty).Trim() + " " + (Apellidos ?? string.Empty).Trim()).Trim()
                    : NombreCompleto;
            }
        }
    }

    /// <summary>Cuerpo de POST /api/auth/paciente/login.</summary>
    public class LoginPacienteRequest
    {
        public string Correo { get; set; }
        public string Password { get; set; }

        /// <summary>Mismo dato que Password, con el nombre que usa el Portal Web.</summary>
        public string Clave { get; set; }
    }
}
