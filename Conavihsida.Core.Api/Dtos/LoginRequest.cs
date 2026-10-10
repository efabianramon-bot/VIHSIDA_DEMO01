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
}
