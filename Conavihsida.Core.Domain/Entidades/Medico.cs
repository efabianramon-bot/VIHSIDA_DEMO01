namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>Extension de un Usuario con rol Medico. Tabla: medicos.</summary>
    public class Medico
    {
        public int Id { get; set; }
        public int UsuarioId { get; set; }
        public string Especialidad { get; set; }
        public bool Activo { get; set; }

        /// <summary>Solo lectura: lo llena el repositorio con un JOIN a usuarios.</summary>
        public string NombreCompleto { get; set; }
    }
}
