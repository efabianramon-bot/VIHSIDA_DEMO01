using System.Web.Http;

namespace Conavihsida.Core.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Logging, CORS y llave de API envuelven TODAS las peticiones,
            // antes de llegar a cualquier controlador. El orden importa:
            // CORS va antes que la llave (ver CorsHandler) y el log de
            // transacciones despues (ver TransaccionesHandler).
            config.MessageHandlers.Add(new Handlers.LoggingHandler());
            config.MessageHandlers.Add(new Handlers.CorsHandler());
            config.MessageHandlers.Add(new Handlers.ApiKeyHandler());
            config.MessageHandlers.Add(new Handlers.TransaccionesHandler());

            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = System.Web.Http.RouteParameter.Optional }
            );

            // JSON "limpio" (camelCase) en vez del XML por defecto de WebAPI.
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
            config.Formatters.Remove(config.Formatters.XmlFormatter);
        }
    }
}
