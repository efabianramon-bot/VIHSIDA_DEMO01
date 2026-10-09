using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>Identidad civil del paciente (login del Portal Web). Tabla: pacientes.</summary>
    public class Paciente
    {
        public int Id { get; set; }
        public string Correo { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string NombreCompleto { get; set; }
        public string Cedula { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaRegistro { get; set; }
    }
}
