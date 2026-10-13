using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IConstanciaRepository
    {
        /// <summary>
        /// Guarda la constancia con sus detalles en una sola transaccion y
        /// la devuelve tal como quedo (con Id, Numero y NombrePaciente).
        /// null si el paciente no existe.
        /// </summary>
        ConstanciaPaciente Insertar(ConstanciaPaciente constancia);

        /// <summary>Constancias del paciente con sus detalles, la mas reciente primero.</summary>
        List<ConstanciaPaciente> ObtenerPorPaciente(int pacienteId);

        /// <summary>null si no existe o no es de ese paciente.</summary>
        ConstanciaPaciente Obtener(int pacienteId, int constanciaId);
    }
}
