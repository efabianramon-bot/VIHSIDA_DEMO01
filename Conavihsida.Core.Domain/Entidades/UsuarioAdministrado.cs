using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Un empleado tal como lo ve el Administrador al gestionar usuarios.
    /// Nunca lleva la contraseña ni su hash.
    /// </summary>
    public class UsuarioAdministrado
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }

        /// <summary>Ver Catalogos.NombresRol. null si no tiene ninguno.</summary>
        public string Rol { get; set; }

        /// <summary>null = sin filial asignada.</summary>
        public int? FilialId { get; set; }
        public string FilialNombre { get; set; }

        public bool Activo { get; set; }

        /// <summary>Bloqueado por intentos fallidos de inicio de sesion.</summary>
        public bool Bloqueado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }
    }

    /// <summary>Tabla: Filiales.</summary>
    public class Filial
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Activa { get; set; }
    }
}
