using System.Linq;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Validacion de subsidio para Caja. Respalda /api/caja/subsidio.
    ///
    /// Regla acordada con Caja para el MVP: el paciente con expediente
    /// Activo tiene subsidio del 100 % (RN-02), sin fecha de vencimiento.
    /// Sin expediente, o con el expediente Inactivo o Transferido, no tiene.
    /// </summary>
    public class SubsidioService
    {
        // La cedula dominicana tiene 11 digitos (000-0000000-0).
        private const int LargoCedula = 11;
        private const int PorcentajeSubsidio = 100;

        private readonly IExpedienteRepository _expedientes;
        private readonly AuditoriaService _auditoria;

        public SubsidioService(IExpedienteRepository expedientes, AuditoriaService auditoria)
        {
            _expedientes = expedientes;
            _auditoria = auditoria;
        }

        /// <summary>
        /// Acepta la cedula con o sin guiones. Devuelve null si no tiene
        /// 11 digitos; si el paciente no existe, devuelve Existe = false.
        /// </summary>
        public SubsidioVista ConsultarPorCedula(string cedula, int usuarioId, string terminal)
        {
            string digitos = new string((cedula ?? string.Empty).Where(char.IsDigit).ToArray());
            if (digitos.Length != LargoCedula)
            {
                return null;
            }

            var paciente = _expedientes.BuscarPaciente(digitos, null);
            bool tieneSubsidio = paciente != null && paciente.EstadoExpediente == EstadosExpediente.Activo;

            // En la bitacora queda quien consulto y a que paciente, pero no
            // la cedula ni el resultado: decir "tiene subsidio" junto al
            // PacienteID seria revelar su condicion fuera del expediente.
            _auditoria.Registrar(usuarioId, paciente == null ? (int?)null : paciente.PacienteId, terminal,
                "ValidarSubsidio", "pacientes", paciente == null ? (int?)null : paciente.PacienteId,
                paciente == null ? "existe=false" : null);

            return new SubsidioVista
            {
                Existe = paciente != null,
                TieneSubsidio = tieneSubsidio,
                Porcentaje = tieneSubsidio ? PorcentajeSubsidio : 0,
                VigenteHasta = null
            };
        }
    }
}
