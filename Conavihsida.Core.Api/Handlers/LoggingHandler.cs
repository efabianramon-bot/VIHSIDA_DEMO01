using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;

namespace Conavihsida.Core.Api.Handlers
{
    /// <summary>
    /// Registro TECNICO (no confundir con la tabla Auditoria, que es de
    /// NEGOCIO). Aqui solo queda: metodo, ruta, codigo de respuesta,
    /// cuanto tardo, y el detalle si algo truena. Nunca se loguea el
    /// cuerpo de la peticion/respuesta: podria traer datos clinicos o
    /// contraseñas y violaria RN-01.
    /// </summary>
    public class LoggingHandler : DelegatingHandler
    {
        private static readonly Logger Log = LogManager.GetLogger("CoreApi");

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cronometro = Stopwatch.StartNew();
            try
            {
                var respuesta = await base.SendAsync(request, cancellationToken);
                cronometro.Stop();

                Log.Info("{0} {1} -> {2} ({3} ms)",
                    request.Method, RutaSinCedula(request),
                    (int)respuesta.StatusCode, cronometro.ElapsedMilliseconds);

                return respuesta;
            }
            catch (Exception ex)
            {
                cronometro.Stop();
                Log.Error(ex, "{0} {1} -> EXCEPCION ({2} ms)",
                    request.Method, RutaSinCedula(request), cronometro.ElapsedMilliseconds);
                throw;
            }
        }

        // La cedula identifica a una persona: en el log queda que se
        // busco por cedula, pero no cual.
        private static string RutaSinCedula(HttpRequestMessage request)
        {
            return Regex.Replace(request.RequestUri.PathAndQuery,
                @"([?&]cedula=)[^&]*", "$1***", RegexOptions.IgnoreCase);
        }
    }
}
