using Conavihsida.Core.Data;
using NLog;

namespace Conavihsida.Core.Api
{
    /// <summary>
    /// Conecta el log de base de datos de la capa Data con NLog. La capa
    /// Data no referencia NLog; aqui se le dice donde escribir. El archivo
    /// de destino lo decide NLog.config (logger "BaseDatos").
    /// </summary>
    public static class LogConfig
    {
        private static readonly Logger LogBaseDatos = LogManager.GetLogger("BaseDatos");

        public static void Register()
        {
            RegistroBaseDatos.Escritor = (nivel, mensaje) =>
            {
                if (nivel == NivelRegistro.Error)
                {
                    LogBaseDatos.Error(mensaje);
                }
                else if (nivel == NivelRegistro.Advertencia)
                {
                    LogBaseDatos.Warn(mensaje);
                }
                else
                {
                    LogBaseDatos.Info(mensaje);
                }
            };
        }
    }
}
