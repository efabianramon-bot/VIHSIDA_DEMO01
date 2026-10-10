using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Security;
using Newtonsoft.Json;

namespace Conavihsida.Core.Empleados.Seguridad
{
    /// <summary>
    /// Lo que se recuerda del empleado entre una pantalla y otra. Viaja en
    /// la cookie de autenticacion de ASP.NET, que va cifrada y firmada: el
    /// navegador no puede leerla ni cambiarse el rol. Nunca incluye la
    /// contraseña.
    /// </summary>
    public class SesionEmpleado
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string NombreCompleto { get; set; }
        public List<string> Roles { get; set; }

        public static void Iniciar(HttpResponseBase respuesta, SesionEmpleado sesion)
        {
            var ahora = DateTime.Now;
            var ticket = new FormsAuthenticationTicket(
                1, sesion.Username, ahora, ahora.Add(FormsAuthentication.Timeout), false,
                JsonConvert.SerializeObject(sesion), FormsAuthentication.FormsCookiePath);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, FormsAuthentication.Encrypt(ticket))
            {
                HttpOnly = true,
                Secure = FormsAuthentication.RequireSSL,
                Path = FormsAuthentication.FormsCookiePath
            };
            respuesta.Cookies.Add(cookie);
        }

        public static void Cerrar()
        {
            FormsAuthentication.SignOut();
        }

        /// <summary>null si la cookie no trae una sesion valida.</summary>
        public static SesionEmpleado Leer(FormsAuthenticationTicket ticket)
        {
            if (ticket == null || string.IsNullOrEmpty(ticket.UserData))
            {
                return null;
            }

            try
            {
                var sesion = JsonConvert.DeserializeObject<SesionEmpleado>(ticket.UserData);
                if (sesion == null || sesion.Id <= 0)
                {
                    return null;
                }
                sesion.Roles = sesion.Roles ?? new List<string>();
                return sesion;
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
