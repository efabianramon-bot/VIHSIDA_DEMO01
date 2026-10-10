namespace Conavihsida.Core.Empleados.Models
{
    public class LoginViewModel
    {
        public string Usuario { get; set; }
        public string Clave { get; set; }

        /// <summary>Mensaje para mostrar cuando el login falla.</summary>
        public string Error { get; set; }

        /// <summary>A donde volver despues de entrar; solo rutas de esta aplicacion.</summary>
        public string ReturnUrl { get; set; }
    }

    /// <summary>Lo que responde POST /api/auth/login cuando las credenciales son correctas.</summary>
    public class UsuarioApi
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public System.Collections.Generic.List<string> Roles { get; set; }
    }
}
