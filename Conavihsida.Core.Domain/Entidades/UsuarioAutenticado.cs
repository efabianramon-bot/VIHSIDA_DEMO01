using System.Collections.Generic;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Lo que el CORE devuelve tras un login correcto. Nunca lleva el hash
    /// de la contraseña. Con esto el Gateway arma los claims del JWT
    /// (RF-INT-01): el CORE valida credenciales, el Gateway firma el token.
    /// </summary>
    public class UsuarioAutenticado
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public List<string> Roles { get; set; }
    }
}
