using System;
using System.Configuration;
using System.Net.Http;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;
using NLog;

namespace Conavihsida.Core.Api.Seguridad
{
    /// <summary>
    /// RF-CORE-05: un intento de entrar donde no corresponde tambien es una
    /// operacion auditable. Lo usan los filtros de seguridad al responder
    /// 403. Si la bitacora falla, el rechazo se devuelve igual: nunca se
    /// deja pasar una peticion por un error de auditoria.
    /// </summary>
    internal static class AuditoriaAccesos
    {
        private static readonly Logger Log = LogManager.GetLogger("CoreApi");

        public static void RegistrarRechazo(HttpRequestMessage request, IdentidadLlamada identidad)
        {
            try
            {
                string cadena = ConfigurationManager.ConnectionStrings["ConavihsidaCoreDb"].ConnectionString;
                new AuditoriaService(new AuditoriaRepository(cadena)).Registrar(
                    identidad.UsuarioId, identidad.PacienteId, IdentidadLlamada.LeerTerminal(request),
                    "AccesoDenegado", "api", null,
                    string.Format("ruta={0} {1};roles={2}", request.Method, request.RequestUri.AbsolutePath,
                        string.Join(",", identidad.Roles)));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "No se pudo auditar un acceso denegado");
            }
        }
    }
}
