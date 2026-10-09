using System.Collections.Generic;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IResultadoRepository
    {
        /// <summary>
        /// La implementacion debe leer de la vista vw_resultados_paciente,
        /// NUNCA de la tabla resultados_lab (RN-01).
        /// </summary>
        List<ResultadoPacienteVista> ObtenerResultadosPaciente(int pacienteId);
    }
}
