namespace Conavihsida.Core.Api.Dtos
{
    /// <summary>Cuerpo de POST /api/usuarios.</summary>
    public class CrearUsuarioRequest
    {
        /// <summary>Con el que inicia sesion. No se puede cambiar despues.</summary>
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        /// <summary>Administrador, Medico, Farmaceutico, Auditor o Cajero.</summary>
        public string Rol { get; set; }

        /// <summary>null = sin filial asignada.</summary>
        public int? FilialId { get; set; }
        public string Password { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/usuarios/{id}.</summary>
    public class ActualizarUsuarioRequest
    {
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Rol { get; set; }
        public int? FilialId { get; set; }

        /// <summary>Opcional: solo si se quiere cambiar la contraseña.</summary>
        public string Password { get; set; }
    }

    /// <summary>Cuerpo de PUT /api/usuarios/{id}/estado.</summary>
    public class CambiarEstadoUsuarioRequest
    {
        public bool Activo { get; set; }
    }
}
