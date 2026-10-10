using System;
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
    /// CORS para que un navegador pueda llamar a esta API desde otro
    /// origen (el Portal Web). Solo responde a los origenes listados en
    /// CoreApi:CorsOrigenes de Web.config, separados por coma; a cualquier
    /// otro no le agrega encabezados y el navegador bloquea la respuesta.
    /// Va ANTES de ApiKeyHandler porque el preflight (OPTIONS) que manda
    /// el navegador nunca trae la llave.
    /// </summary>
    public class CorsHandler : DelegatingHandler
    {
        private const string MetodosPermitidos = "GET, POST, PUT, OPTIONS";
        private const string HeadersPermitidos =
            "Content-Type, Authorization, X-Api-Key, X-Usuario-Id, X-Roles, X-Paciente-Id";

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string origen = LeerOrigen(request);
            bool permitido = origen != null && OrigenesPermitidos().Contains(origen, StringComparer.OrdinalIgnoreCase);

            bool esPreflight = request.Method == HttpMethod.Options
                && request.Headers.Contains("Access-Control-Request-Method");

            HttpResponseMessage respuesta;
            if (esPreflight)
            {
                respuesta = request.CreateResponse(permitido ? HttpStatusCode.NoContent : HttpStatusCode.Forbidden);
            }
            else
            {
                respuesta = await base.SendAsync(request, cancellationToken);
            }

            if (permitido)
            {
                respuesta.Headers.Add("Access-Control-Allow-Origin", origen);
                respuesta.Headers.Add("Vary", "Origin");
                if (esPreflight)
                {
                    respuesta.Headers.Add("Access-Control-Allow-Methods", MetodosPermitidos);
                    respuesta.Headers.Add("Access-Control-Allow-Headers", HeadersPermitidos);
                    respuesta.Headers.Add("Access-Control-Max-Age", "600");
                }
            }

            return respuesta;
        }

        private static string LeerOrigen(HttpRequestMessage request)
        {
            IEnumerable<string> valores;
            return request.Headers.TryGetValues("Origin", out valores) ? valores.FirstOrDefault() : null;
        }

        private static string[] OrigenesPermitidos()
        {
            string configurados = ConfigurationManager.AppSettings["CoreApi:CorsOrigenes"] ?? string.Empty;
            return configurados
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(o => o.Trim().TrimEnd('/'))
                .Where(o => o.Length > 0)
                .ToArray();
        }
    }
}
