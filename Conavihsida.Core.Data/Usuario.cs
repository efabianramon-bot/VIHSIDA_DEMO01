using System;
using System.Collections.Generic;

namespace Conavihsida.Core.Models
{
    public class Usuario
    {
        public int UsuarioID { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }
        public int IntentosFallidos { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }

    public class ResultadoLogin
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; }
        public Usuario Usuario { get; set; }
    }
}