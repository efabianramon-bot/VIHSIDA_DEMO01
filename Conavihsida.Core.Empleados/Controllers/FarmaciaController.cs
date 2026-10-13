using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    // RF-CORE-07: Portal Farmacia. Aqui nunca aparece informacion clinica
    // ni la identidad del paciente: la receta solo trae medicamento,
    // cantidad y codigo serologico (RN-01).
    [AutorizarRol(NombresRol.Farmaceutico)]
    public class FarmaciaController : Controller
    {
        private CoreApiClient Api
        {
            get { return new CoreApiClient(EmpleadoPrincipal.SesionActual(HttpContext), Request.UserHostAddress); }
        }

        public async Task<ActionResult> Index()
        {
            var api = Api;
            var tareaLotes = api.GetAsync<List<LoteApi>>("api/farmacia/lotes");
            var tareaRecetas = api.GetAsync<List<RecetaApi>>("api/farmacia/recetas");
            var tareaMedicamentos = api.GetAsync<List<MedicamentoApi>>("api/farmacia/medicamentos");
            await Task.WhenAll(tareaLotes, tareaRecetas, tareaMedicamentos);

            return View(new FarmaciaInicioViewModel
            {
                Lotes = tareaLotes.Result.Datos ?? new List<LoteApi>(),
                RecetasPendientes = tareaRecetas.Result.Datos ?? new List<RecetaApi>(),
                Medicamentos = tareaMedicamentos.Result.Datos ?? new List<MedicamentoApi>(),
                Error = TempData["Error"] as string
                    ?? (tareaLotes.Result.Exitosa ? null : tareaLotes.Result.Error),
                Exito = TempData["Exito"] as string
            });
        }

        [HttpGet]
        public ActionResult BuscarReceta(string q)
        {
            int recetaId;
            if (!int.TryParse((q ?? string.Empty).Trim(), out recetaId) || recetaId <= 0)
            {
                TempData["Error"] = "Escribe el número de la receta.";
                return RedirectToAction("Index");
            }
            return RedirectToAction("Receta", new { id = recetaId });
        }

        [HttpGet]
        public async Task<ActionResult> NuevoLote()
        {
            return View(await ArmarNuevoLoteAsync(new RegistrarLoteForm { ExistenciaMinima = 0 }, null));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> NuevoLote(RegistrarLoteForm lote)
        {
            // Lo unico que se revisa aqui es que los campos obligatorios
            // vengan; las reglas (vencido, maximo, repetido) son de la API.
            if (lote.MedicamentoId == null || lote.FechaCaducidad == null || lote.Cantidad == null)
            {
                return View(await ArmarNuevoLoteAsync(lote, "Indica el medicamento, la fecha de caducidad y la cantidad recibida."));
            }

            var respuesta = await Api.PostAsync<LoteApi>("api/farmacia/lotes", new
            {
                medicamentoId = lote.MedicamentoId.Value,
                numeroLote = lote.NumeroLote,
                fechaCaducidad = lote.FechaCaducidad.Value,
                cantidad = lote.Cantidad.Value,
                existenciaMinima = lote.ExistenciaMinima ?? 0,
                existenciaMaxima = lote.ExistenciaMaxima
            });

            if (!respuesta.Exitosa)
            {
                return View(await ArmarNuevoLoteAsync(lote, respuesta.Error));
            }

            TempData["Exito"] = "Lote " + respuesta.Datos.NumeroLote + " registrado con "
                + respuesta.Datos.ExistenciaActual + " unidades.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<ActionResult> Receta(int id)
        {
            var modelo = await ArmarRecetaAsync(id);
            if (modelo == null)
            {
                TempData["Error"] = "No existe la receta n.º " + id + ".";
                return RedirectToAction("Index");
            }

            modelo.Exito = TempData["Exito"] as string;
            modelo.Error = TempData["Error"] as string;
            return View(modelo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> Despachar(int id)
        {
            var api = Api;
            var respuesta = await api.PostAsync<List<MovimientoApi>>("api/farmacia/recetas/" + id + "/despachar", null);
            if (!respuesta.Exitosa)
            {
                TempData["Error"] = respuesta.Error;
                return RedirectToAction("Receta", new { id });
            }

            // Se dice de que lotes salio, para que el farmaceutico tome las
            // unidades del lote correcto.
            var lotes = (await api.GetAsync<List<LoteApi>>("api/farmacia/lotes")).Datos ?? new List<LoteApi>();
            var partes = respuesta.Datos.Select(m =>
            {
                var lote = lotes.Find(l => l.Id == m.LoteId);
                return m.Cantidad + " del lote " + (lote == null ? "n.º " + m.LoteId : lote.NumeroLote);
            });
            TempData["Exito"] = "Receta despachada: " + string.Join(", ", partes) + ".";
            return RedirectToAction("Receta", new { id });
        }

        // Constancia imprimible de lo que se entrego. Igual que el resto de
        // Farmacia, no lleva la identidad del paciente: solo el codigo.
        [HttpGet]
        public async Task<ActionResult> Comprobante(int id)
        {
            var api = Api;
            var receta = await api.GetAsync<RecetaApi>("api/farmacia/recetas/" + id);
            if (!receta.Exitosa || receta.Datos == null)
            {
                TempData["Error"] = "No existe la receta n.º " + id + ".";
                return RedirectToAction("Index");
            }

            var tareaSalidas = api.GetAsync<List<MovimientoApi>>("api/farmacia/recetas/" + id + "/despacho");
            var tareaMedicamentos = api.GetAsync<List<MedicamentoApi>>("api/farmacia/medicamentos");
            await Task.WhenAll(tareaSalidas, tareaMedicamentos);

            var salidas = tareaSalidas.Result.Datos ?? new List<MovimientoApi>();
            if (salidas.Count == 0)
            {
                TempData["Error"] = "La receta n.º " + id + " todavía no se ha despachado: no hay comprobante.";
                return RedirectToAction("Receta", new { id });
            }

            var medicamento = (tareaMedicamentos.Result.Datos ?? new List<MedicamentoApi>())
                .Find(m => m.Id == receta.Datos.MedicamentoId);

            return View(new ComprobanteDespachoViewModel
            {
                Receta = receta.Datos,
                MedicamentoNombre = medicamento == null
                    ? "Medicamento n.º " + receta.Datos.MedicamentoId : medicamento.NombreCompleto,
                Salidas = salidas
            });
        }

        private async Task<RegistrarLoteViewModel> ArmarNuevoLoteAsync(RegistrarLoteForm lote, string error)
        {
            var medicamentos = await Api.GetAsync<List<MedicamentoApi>>("api/farmacia/medicamentos");
            return new RegistrarLoteViewModel
            {
                Lote = lote,
                Medicamentos = medicamentos.Datos ?? new List<MedicamentoApi>(),
                Error = error ?? (medicamentos.Exitosa ? null : medicamentos.Error)
            };
        }

        /// <summary>null si la receta no existe.</summary>
        private async Task<RecetaViewModel> ArmarRecetaAsync(int recetaId)
        {
            var api = Api;
            var receta = await api.GetAsync<RecetaApi>("api/farmacia/recetas/" + recetaId);
            if (!receta.Exitosa || receta.Datos == null)
            {
                return null;
            }

            var tareaLotes = api.GetAsync<List<LoteApi>>("api/farmacia/lotes?medicamentoId=" + receta.Datos.MedicamentoId);
            var tareaMedicamentos = api.GetAsync<List<MedicamentoApi>>("api/farmacia/medicamentos");
            await Task.WhenAll(tareaLotes, tareaMedicamentos);

            var medicamento = (tareaMedicamentos.Result.Datos ?? new List<MedicamentoApi>())
                .Find(m => m.Id == receta.Datos.MedicamentoId);

            return new RecetaViewModel
            {
                Receta = receta.Datos,
                MedicamentoNombre = medicamento == null
                    ? "Medicamento n.º " + receta.Datos.MedicamentoId : medicamento.NombreCompleto,
                // La API ya los devuelve ordenados por caducidad.
                LotesDisponibles = (tareaLotes.Result.Datos ?? new List<LoteApi>())
                    .Where(l => l.Despachable).ToList()
            };
        }
    }
}
