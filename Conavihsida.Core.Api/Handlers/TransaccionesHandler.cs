using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using NLog;

namespace Conavihsida.Core.Api.Handlers
{
    /// <summary>
    /// Log de TRANSACCIONES (de negocio): una linea por cada operacion que
    /// llega a un controlador, con quien la hizo, desde donde, que operacion
    /// fue, sobre que ruta y si se completo o fue rechazada (y por que).
    ///
    /// No confundir con LoggingHandler, que es el log tecnico de todas las
    /// peticiones, ni con la tabla Auditoria, que solo guarda lo que si se
    /// hizo. Aqui tambien quedan los rechazos: un 403, un 409 por stock
    /// insuficiente, una receta que ya no esta activa.
    ///
    /// Nunca se registra el cuerpo de la peticion ni de la respuesta
    /// (RN-01); el motivo sale solo del mensaje de error que ya se le
    /// devuelve a quien llamo. Va DESPUES de ApiKeyHandler: lo que no trae
    /// la llave no es una operacion de negocio.
    /// </summary>
    public class TransaccionesHandler : DelegatingHandler
    {
        private static readonly Logger Log = LogManager.GetLogger("Transacciones");

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var cronometro = Stopwatch.StartNew();
            HttpResponseMessage respuesta;
            try
            {
                respuesta = await base.SendAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                cronometro.Stop();
                Registrar(request, "ERROR", 500, cronometro.ElapsedMilliseconds, ex.GetType().Name);
                throw;
            }
            cronometro.Stop();

            int codigo = (int)respuesta.StatusCode;
            string resultado = codigo < 400 ? "COMPLETADA" : codigo < 500 ? "RECHAZADA" : "ERROR";
            Registrar(request, resultado, codigo, cronometro.ElapsedMilliseconds, LeerMotivo(respuesta, codigo));

            return respuesta;
        }

        private static void Registrar(HttpRequestMessage request, string resultado, int codigo,
                                      long milisegundos, string motivo)
        {
            string operacion = NombreOperacion(request);
            if (operacion == null)
            {
                // La ruta no corresponde a ninguna operacion (404 de ruteo).
                return;
            }

            var identidad = IdentidadLlamada.Leer(request);
            string linea = string.Format(
                "{0} | usuario={1} roles={2} paciente={3} terminal={4} | {5} | {6} {7} | {8} | {9} ms{10}",
                resultado,
                Texto(identidad.UsuarioId),
                identidad.Roles.Count > 0 ? string.Join(",", identidad.Roles) : "-",
                Texto(identidad.PacienteId),
                IdentidadLlamada.LeerTerminal(request),
                operacion,
                request.Method, request.RequestUri.AbsolutePath,
                codigo,
                milisegundos,
                string.IsNullOrEmpty(motivo) ? string.Empty : " | " + motivo);

            if (resultado == "COMPLETADA")
            {
                Log.Info(linea);
            }
            else if (resultado == "RECHAZADA")
            {
                Log.Warn(linea);
            }
            else
            {
                Log.Error(linea);
            }
        }

        // "Farmacia.DespacharReceta": controlador y accion, sin el sufijo Controller.
        private static string NombreOperacion(HttpRequestMessage request)
        {
            var accion = request.GetActionDescriptor();
            if (accion == null)
            {
                return null;
            }
            return accion.ControllerDescriptor.ControllerName + "." + accion.ActionName;
        }

        // Los rechazos de esta API responden { error = "..." } o el
        // { Message = "..." } de BadRequest. En un error 500 no se copia el
        // detalle: puede traer texto interno; ese queda en el log tecnico.
        private static string LeerMotivo(HttpResponseMessage respuesta, int codigo)
        {
            if (codigo < 400 || codigo >= 500)
            {
                return null;
            }

            var contenido = respuesta.Content as ObjectContent;
            if (contenido == null || contenido.Value == null)
            {
                return null;
            }

            var errorHttp = contenido.Value as HttpError;
            if (errorHttp != null)
            {
                return errorHttp.Message;
            }

            var propiedad = contenido.Value.GetType().GetProperty("error");
            return propiedad == null ? null : propiedad.GetValue(contenido.Value) as string;
        }

        private static string Texto(int? valor)
        {
            return valor == null ? "-" : valor.Value.ToString();
        }
    }
}
