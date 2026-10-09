using System.Collections.Generic;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Respalda /pacientes/{id}/resultados del Portal Web. Devuelve
    /// unicamente ResultadoPacienteVista, que no tiene ningun campo de
    /// valor clinico (RN-01): la confidencialidad se garantiza aqui, en
    /// el backend, no ocultando datos en la pantalla.
    /// </summary>
    public class ResultadoService
    {
        private readonly IResultadoRepository _resultados;
        private readonly AuditoriaService _auditoria;

        public ResultadoService(IResultadoRepository resultados, AuditoriaService auditoria)
        {
            _resultados = resultados;
            _auditoria = auditoria;
        }

        public List<ResultadoPacienteVista> ObtenerResultadosPaciente(int pacienteId, string terminal)
        {
            var lista = _resultados.ObtenerResultadosPaciente(pacienteId);

            _auditoria.Registrar(null, pacienteId, terminal, "ConsultarResultados",
                "resultados_lab", null, null);

            return lista;
        }
    }
}
