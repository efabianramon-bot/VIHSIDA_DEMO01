using System;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Conavihsida.Core.Empleados.Seguridad;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Conavihsida.Core.Empleados.Servicios
{
    /// <summary>
    /// Unica puerta de esta aplicacion hacia los datos: todo se pide a
    /// Conavihsida.Core.Api por HTTP (RNF-03). En cada llamada va la llave
    /// de la API y, si hay sesion, quien es el empleado y sus roles, igual
    /// que lo haria el Gateway despues de validar un JWT.
    /// </summary>
    public class CoreApiClient
    {
        // Un solo HttpClient para toda la aplicacion: crear uno por
        // peticion agota los sockets.
        private static readonly HttpClient Http = CrearHttp();

        private static readonly JsonSerializerSettings Json = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly SesionEmpleado _sesion;
        private readonly string _terminal;

        /// <param name="sesion">null solo para el login.</param>
        /// <param name="terminal">IP del navegador; queda en la auditoria de la API.</param>
        public CoreApiClient(SesionEmpleado sesion, string terminal)
        {
            _sesion = sesion;
            _terminal = terminal;
        }

        public Task<RespuestaApi<T>> GetAsync<T>(string ruta)
        {
            return EnviarAsync<T>(HttpMethod.Get, ruta, null);
        }

        public Task<RespuestaApi<T>> PostAsync<T>(string ruta, object cuerpo)
        {
            return EnviarAsync<T>(HttpMethod.Post, ruta, cuerpo);
        }

        public Task<RespuestaApi<T>> PutAsync<T>(string ruta, object cuerpo)
        {
            return EnviarAsync<T>(HttpMethod.Put, ruta, cuerpo);
        }

        private async Task<RespuestaApi<T>> EnviarAsync<T>(HttpMethod metodo, string ruta, object cuerpo)
        {
            var peticion = new HttpRequestMessage(metodo, ruta);
            peticion.Headers.TryAddWithoutValidation("X-Api-Key", ConfigurationManager.AppSettings["CoreApi:ApiKey"]);
            if (!string.IsNullOrEmpty(_terminal))
            {
                peticion.Headers.TryAddWithoutValidation("X-Forwarded-For", _terminal);
            }
            if (_sesion != null)
            {
                peticion.Headers.TryAddWithoutValidation("X-Usuario-Id", _sesion.Id.ToString());
                peticion.Headers.TryAddWithoutValidation("X-Roles", string.Join(",", _sesion.Roles));
            }
            if (cuerpo != null)
            {
                peticion.Content = new StringContent(
                    JsonConvert.SerializeObject(cuerpo, Json), Encoding.UTF8, "application/json");
            }

            HttpResponseMessage respuesta;
            string texto;
            try
            {
                respuesta = await Http.SendAsync(peticion).ConfigureAwait(false);
                texto = await respuesta.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
            {
                return new RespuestaApi<T>
                {
                    Codigo = 0,
                    Error = "No se pudo conectar con el servicio central. Intenta de nuevo en un momento."
                };
            }

            var resultado = new RespuestaApi<T> { Codigo = (int)respuesta.StatusCode };
            if (resultado.Exitosa)
            {
                if (!string.IsNullOrWhiteSpace(texto))
                {
                    resultado.Datos = JsonConvert.DeserializeObject<T>(texto, Json);
                }
            }
            else
            {
                resultado.Error = LeerError(texto, resultado.Codigo);
            }
            return resultado;
        }

        // La API responde { "error": "..." } en sus rechazos y
        // { "message": "..." } en los 400 de validacion.
        private static string LeerError(string texto, int codigo)
        {
            if (codigo >= 500)
            {
                return "El servicio central tuvo un problema. Intenta de nuevo en un momento.";
            }

            try
            {
                var json = JObject.Parse(texto);
                string mensaje = (string)json["error"] ?? (string)json["message"];
                if (!string.IsNullOrWhiteSpace(mensaje))
                {
                    return mensaje;
                }
            }
            catch (JsonException)
            {
            }

            return codigo == 404 ? "No se encontró lo que buscas." : "La operación fue rechazada.";
        }

        private static HttpClient CrearHttp()
        {
            return new HttpClient
            {
                BaseAddress = new Uri(ConfigurationManager.AppSettings["CoreApi:BaseUrl"]),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }
    }
}
