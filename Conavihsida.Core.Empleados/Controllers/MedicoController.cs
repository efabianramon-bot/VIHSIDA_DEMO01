using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Conavihsida.Core.Empleados.Models;
using Conavihsida.Core.Empleados.Seguridad;
using Conavihsida.Core.Empleados.Servicios;

namespace Conavihsida.Core.Empleados.Controllers
{
    // RF-CORE-06: Portal Medico. Aqui solo se decide que se muestra; quien
    // protege los datos clinicos es la API, que exige el rol Medico en cada
    // llamada (RN-01).
    [AutorizarRol(NombresRol.Medico)]
    public class MedicoController : Controller
    {
        private const int LargoCedula = 11;

        private CoreApiClient Api
        {
            get { return new CoreApiClient(EmpleadoPrincipal.SesionActual(HttpContext), Request.UserHostAddress); }
        }

        public async Task<ActionResult> Index()
        {
            return View(await ArmarInicioAsync(null, TempData["Error"] as string, null));
        }

        // La busqueda acepta una cedula (11 digitos, con o sin guiones) o
        // un codigo serologico.
        [HttpGet]
        public async Task<ActionResult> Buscar(string q)
        {
            q = (q ?? string.Empty).Trim();
            if (q.Length == 0)
            {
                return View("Index", await ArmarInicioAsync(q, "Escribe una cédula o un código de expediente.", null));
            }

            string digitos = new string(q.Where(char.IsDigit).ToArray());
            bool pareceCedula = digitos.Length == LargoCedula && q.All(c => char.IsDigit(c) || c == '-' || c == ' ');
            if (!pareceCedula)
            {
                return RedirectToAction("Expediente", new { codigo = q.ToUpperInvariant() });
            }

            var paciente = await Api.GetAsync<PacienteApi>("api/clinica/pacientes?cedula=" + digitos);
            if (!paciente.Exitosa)
            {
                string error = paciente.Codigo == 404 ? "No hay ningún paciente registrado con esa cédula." : paciente.Error;
                return View("Index", await ArmarInicioAsync(q, error, null));
            }

            if (!string.IsNullOrEmpty(paciente.Datos.CodigoSerologico))
            {
                return RedirectToAction("Expediente", new { codigo = paciente.Datos.CodigoSerologico });
            }

            return View("Index", await ArmarInicioAsync(q, null, paciente.Datos));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> AbrirExpediente(int pacienteId)
        {
            var respuesta = await Api.PostAsync<ExpedienteApi>("api/clinica/expediente", new { pacienteId });
            if (!respuesta.Exitosa)
            {
                TempData["Error"] = respuesta.Error;
                return RedirectToAction("Index");
            }

            TempData["Exito"] = "Expediente abierto. Su código serológico es " + respuesta.Datos.CodigoSerologico + ".";
            return RedirectToAction("Expediente", new { codigo = respuesta.Datos.CodigoSerologico });
        }

        [HttpGet]
        public async Task<ActionResult> Expediente(string codigo)
        {
            var modelo = await ArmarExpedienteAsync(codigo);
            if (modelo == null)
            {
                TempData["Error"] = "No existe un expediente con el código " + codigo + ".";
                return RedirectToAction("Index");
            }

            modelo.NuevaConsulta = NuevaConsultaForm.Vacio();
            modelo.Exito = TempData["Exito"] as string;
            modelo.Error = TempData["Error"] as string ?? modelo.Error;
            return View(modelo);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> SolicitarPrueba(string codigo, string prueba, string notaPaciente)
        {
            var respuesta = await Api.PostAsync<ResultadoLabApi>(
                "api/clinica/expediente/" + Uri.EscapeDataString(codigo ?? string.Empty) + "/resultados",
                new { prueba, notaPaciente });

            if (respuesta.Exitosa)
            {
                TempData["Exito"] = "Prueba solicitada. El paciente ya la ve como pendiente en su portal.";
            }
            else
            {
                TempData["Error"] = respuesta.Error;
            }
            return RedirectToAction("Expediente", new { codigo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> PublicarResultado(string codigo, int resultadoId, string valorResultado, string notaPaciente)
        {
            var respuesta = await Api.PutAsync<ResultadoLabApi>(
                "api/clinica/resultados/" + resultadoId,
                new { valorResultado, notaPaciente });

            if (respuesta.Exitosa)
            {
                TempData["Exito"] = "Resultado publicado. El paciente lo ve como listo, sin el valor.";
            }
            else
            {
                TempData["Error"] = respuesta.Error;
            }
            return RedirectToAction("Expediente", new { codigo });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<ActionResult> RegistrarConsulta(string codigo, NuevaConsultaForm consulta)
        {
            consulta.Recetas = consulta.Recetas ?? new List<RecetaForm>();

            // Las filas de receta sin medicamento elegido no se envian.
            var recetas = consulta.Recetas
                .Where(r => r != null && r.MedicamentoId > 0)
                .Select(r => new
                {
                    medicamentoId = r.MedicamentoId.Value,
                    esquemaTarv = r.EsquemaTarv,
                    cantidadPrescrita = r.CantidadPrescrita ?? 0
                })
                .ToList();

            var respuesta = await Api.PostAsync<ConsultaApi>(
                "api/clinica/expediente/" + Uri.EscapeDataString(codigo) + "/consultas",
                new
                {
                    diagnosticoDetalle = consulta.DiagnosticoDetalle,
                    notasClinicas = consulta.NotasClinicas,
                    cargaViral = consulta.CargaViral,
                    conteoCd4 = consulta.ConteoCd4,
                    recetas
                });

            if (respuesta.Exitosa)
            {
                int emitidas = respuesta.Datos.Recetas == null ? 0 : respuesta.Datos.Recetas.Count;
                TempData["Exito"] = emitidas == 0
                    ? "Consulta registrada."
                    : "Consulta registrada con " + emitidas + (emitidas == 1 ? " receta." : " recetas.");
                return RedirectToAction("Expediente", new { codigo });
            }

            // Se vuelve a mostrar la pagina con lo que el medico escribio,
            // para que no tenga que teclearlo de nuevo.
            var modelo = await ArmarExpedienteAsync(codigo);
            if (modelo == null)
            {
                TempData["Error"] = respuesta.Error;
                return RedirectToAction("Index");
            }

            while (consulta.Recetas.Count < NuevaConsultaForm.MaximoRecetas)
            {
                consulta.Recetas.Add(new RecetaForm());
            }
            modelo.NuevaConsulta = consulta;
            modelo.Error = respuesta.Error;
            return View("Expediente", modelo);
        }

        // Receta imprimible para entregar al paciente. Lleva su nombre y
        // su cedula (el medico puede verlos), el medicamento y el esquema,
        // pero ningun valor clinico ni el diagnostico.
        [HttpGet]
        public async Task<ActionResult> Receta(string codigo, int id)
        {
            var modelo = await ArmarExpedienteAsync(codigo);
            if (modelo == null)
            {
                TempData["Error"] = "No existe un expediente con el código " + codigo + ".";
                return RedirectToAction("Index");
            }

            var consulta = modelo.Consultas.Find(c => c.Recetas != null && c.Recetas.Any(r => r.Id == id));
            if (consulta == null)
            {
                TempData["Error"] = "La receta n.º " + id + " no pertenece a este expediente.";
                return RedirectToAction("Expediente", new { codigo });
            }

            var receta = consulta.Recetas.First(r => r.Id == id);
            var medicos = await Api.GetAsync<List<MedicoApi>>("api/medicos");

            return View(new RecetaImprimibleViewModel
            {
                Receta = receta,
                Paciente = modelo.Paciente,
                MedicamentoNombre = modelo.NombreMedicamento(receta.MedicamentoId),
                Medico = (medicos.Datos ?? new List<MedicoApi>()).Find(m => m.UsuarioId == consulta.UsuarioMedicoId)
            });
        }

        private async Task<MedicoInicioViewModel> ArmarInicioAsync(string busqueda, string error, PacienteApi sinExpediente)
        {
            var expedientes = await Api.GetAsync<List<ExpedienteApi>>("api/clinica/expedientes");
            return new MedicoInicioViewModel
            {
                Expedientes = expedientes.Datos ?? new List<ExpedienteApi>(),
                Busqueda = busqueda,
                Error = error ?? (expedientes.Exitosa ? null : expedientes.Error),
                PacienteSinExpediente = sinExpediente
            };
        }

        /// <summary>null si el expediente no existe.</summary>
        private async Task<ExpedienteViewModel> ArmarExpedienteAsync(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return null;
            }

            string ruta = "api/clinica/expediente/" + Uri.EscapeDataString(codigo.Trim());
            var expediente = await Api.GetAsync<ExpedienteApi>(ruta);
            if (!expediente.Exitosa || expediente.Datos == null)
            {
                return null;
            }

            // Las cinco consultas restantes no dependen entre si.
            var tareaMedicos = Api.GetAsync<List<MedicoApi>>("api/medicos");
            var tareaPaciente = Api.GetAsync<PacienteApi>("api/clinica/pacientes?pacienteId=" + expediente.Datos.PacienteId);
            var tareaHistorial = Api.GetAsync<List<ConsultaApi>>(ruta + "/historial");
            var tareaResultados = Api.GetAsync<List<ResultadoLabApi>>(ruta + "/resultados");
            var tareaMedicamentos = Api.GetAsync<List<MedicamentoApi>>("api/farmacia/medicamentos");
            await Task.WhenAll(tareaPaciente, tareaHistorial, tareaResultados, tareaMedicamentos, tareaMedicos);

            var responsable = (tareaMedicos.Result.Datos ?? new List<MedicoApi>())
                .Find(m => m.Id == expediente.Datos.MedicoResponsableId);

            return new ExpedienteViewModel
            {
                MedicoResponsable = responsable == null ? null : responsable.NombreCompleto,
                Expediente = expediente.Datos,
                Paciente = tareaPaciente.Result.Datos,
                Consultas = tareaHistorial.Result.Datos ?? new List<ConsultaApi>(),
                Resultados = tareaResultados.Result.Datos ?? new List<ResultadoLabApi>(),
                Medicamentos = tareaMedicamentos.Result.Datos ?? new List<MedicamentoApi>(),
                Error = !tareaHistorial.Result.Exitosa ? tareaHistorial.Result.Error
                    : tareaResultados.Result.Exitosa ? null : tareaResultados.Result.Error
            };
        }
    }
}
