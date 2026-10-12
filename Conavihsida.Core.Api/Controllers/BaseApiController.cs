using System.Configuration;
using System.Net.Http;
using System.Web.Http;
using Conavihsida.Core.Api.Seguridad;
using Conavihsida.Core.Aplicacion.Servicios;
using Conavihsida.Core.Data;

namespace Conavihsida.Core.Api.Controllers
{
    /// <summary>
    /// Arma los repositorios/servicios con el connection string de
    /// Web.config. No usamos un contenedor de Inyeccion de Dependencias
    /// todavia (seria el siguiente paso de pulido, no indispensable para
    /// el MVP); por ahora cada controlador arma lo que necesita.
    /// </summary>
    public abstract class BaseApiController : ApiController
    {
        protected static string ConnectionString
        {
            get { return ConfigurationManager.ConnectionStrings["ConavihsidaCoreDb"].ConnectionString; }
        }

        protected static AuditoriaService CrearAuditoriaService()
        {
            return new AuditoriaService(new AuditoriaRepository(ConnectionString));
        }

        /// <summary>
        /// Para los endpoints que muestran el estado de las recetas sin pasar
        /// por InventarioService (historial y dashboard): aplica antes la
        /// regla de vencimiento, para que no salgan Activas las ya vencidas.
        /// </summary>
        protected void VencerRecetasAntiguas()
        {
            new InventarioService(new InventarioRepository(ConnectionString), CrearAuditoriaService())
                .VencerRecetasAntiguas(ObtenerTerminal());
        }

        /// <summary>Empleado o paciente detras de la peticion, segun el Gateway.</summary>
        protected IdentidadLlamada Identidad
        {
            get { return IdentidadLlamada.Leer(Request); }
        }

        /// <summary>Terminal que va a la auditoria: quien llamo, no un dato sensible.</summary>
        protected string ObtenerTerminal()
        {
            return IdentidadLlamada.LeerTerminal(Request);
        }
    }
}
