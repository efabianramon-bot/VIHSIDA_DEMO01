using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Conavihsida.Core.Api.Handlers
{
    /// <summary>
    /// Proteccion MINIMA para que esta API solo responda a quien tenga la
    /// llave compartida (pensado para que solo el Gateway de Ricardo la
    /// llame). Esto NO reemplaza el JWT del usuario final: ese lo valida
    /// el Gateway antes de reenviar la peticion aqui (RF-INT-01). Si el
    /// equipo decide que Core.Api reciba trafico directo de otros lados,
    /// hay que revisar esta estrategia de seguridad de nuevo.
    /// </summary>
    public class ApiKeyHandler : DelegatingHandler
    {
        private const string NombreHeader = "X-Api-Key";

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string llaveEsperada = ConfigurationManager.AppSettings["CoreApi:ApiKey"];
            IEnumerable<string> valores;

            bool tieneHeader = request.Headers.TryGetValues(NombreHeader, out valores);
            string llaveRecibida = tieneHeader ? valores.FirstOrDefault() : null;

            if (string.IsNullOrEmpty(llaveEsperada) || llaveRecibida != llaveEsperada)
            {
                var respuesta = request.CreateResponse(HttpStatusCode.Unauthorized,
                    new { error = "Falta o es invalido el header X-Api-Key." });
                return Task.FromResult(respuesta);
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
