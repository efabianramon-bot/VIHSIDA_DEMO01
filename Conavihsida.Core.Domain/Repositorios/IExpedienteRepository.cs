using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IExpedienteRepository
    {
        /// <summary>null si no existe.</summary>
        ExpedienteClinico ObtenerPorCodigo(string codigoSerologico);

        /// <summary>null si el paciente todavia no tiene expediente.</summary>
        ExpedienteClinico ObtenerPorPaciente(int pacienteId);

        /// <summary>Todos los expedientes, el mas reciente primero.</summary>
        List<ExpedienteClinico> Listar();

        /// <summary>
        /// Busca un paciente por su cedula (solo digitos) o por su Id; se
        /// usa el criterio que no venga en null. null si no existe.
        /// </summary>
        PacienteClinicoVista BuscarPaciente(string cedulaSoloDigitos, int? pacienteId);

        /// <summary>
        /// Inserta el expediente. Debe devolver false (sin lanzar excepcion)
        /// cuando la base lo rechaza: el paciente o el medico no existen, o
        /// el paciente o el codigo ya tienen expediente.
        /// </summary>
        bool Insertar(ExpedienteClinico expediente);

        /// <summary>
        /// Cambia medico responsable y estado. false si el expediente no
        /// existe o el medico indicado no existe.
        /// </summary>
        bool Actualizar(string codigoSerologico, int? medicoResponsableId, string estado);
    }
}
