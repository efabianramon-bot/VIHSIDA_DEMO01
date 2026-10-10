using System;
using System.Security.Cryptography;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-02: expediente clinico confidencial. Respalda
    /// /api/clinica/expediente, de uso exclusivo del rol Medico (RN-01).
    /// </summary>
    public class ExpedienteService
    {
        // Sin 0/O ni 1/I para que el codigo se pueda dictar sin confusiones.
        private const string AlfabetoCodigo = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int LargoAleatorioCodigo = 10;
        private const int IntentosCodigo = 3;

        private readonly IExpedienteRepository _expedientes;
        private readonly AuditoriaService _auditoria;

        public ExpedienteService(IExpedienteRepository expedientes, AuditoriaService auditoria)
        {
            _expedientes = expedientes;
            _auditoria = auditoria;
        }

        public ExpedienteClinico ObtenerExpediente(string codigoSerologico, int usuarioId, string terminal)
        {
            var expediente = _expedientes.ObtenerPorCodigo(codigoSerologico);
            if (expediente != null)
            {
                Auditar(usuarioId, terminal, "ConsultarExpediente", codigoSerologico);
            }
            return expediente;
        }

        public ResultadoOperacion<ExpedienteClinico> AbrirExpediente(int pacienteId, int? medicoResponsableId,
                                                                     int usuarioId, string terminal)
        {
            if (_expedientes.ObtenerPorPaciente(pacienteId) != null)
            {
                return ResultadoOperacion<ExpedienteClinico>.Fallo("El paciente ya tiene un expediente clínico.");
            }

            var expediente = new ExpedienteClinico
            {
                PacienteId = pacienteId,
                MedicoResponsableId = medicoResponsableId,
                FechaApertura = DateTime.Now,
                Estado = EstadosExpediente.Activo
            };

            // El codigo es aleatorio a proposito: no debe poder deducirse de
            // la cedula, el nombre ni el PacienteID. Se reintenta por si choca.
            bool insertado = false;
            for (int intento = 0; intento < IntentosCodigo && !insertado; intento++)
            {
                expediente.CodigoSerologico = GenerarCodigo();
                insertado = _expedientes.Insertar(expediente);
            }

            if (!insertado)
            {
                return ResultadoOperacion<ExpedienteClinico>.Fallo(
                    "No se pudo abrir el expediente. Verifica que el paciente y el médico responsable existan.");
            }

            Auditar(usuarioId, terminal, "AbrirExpediente", expediente.CodigoSerologico);
            return ResultadoOperacion<ExpedienteClinico>.Ok(expediente);
        }

        public ResultadoOperacion<ExpedienteClinico> ActualizarExpediente(string codigoSerologico,
            int? medicoResponsableId, string estado, int usuarioId, string terminal)
        {
            if (!EstadosExpediente.EsValido(estado))
            {
                return ResultadoOperacion<ExpedienteClinico>.Fallo(
                    "El estado debe ser Activo, Inactivo o Transferido.");
            }

            if (!_expedientes.Actualizar(codigoSerologico, medicoResponsableId, estado))
            {
                return ResultadoOperacion<ExpedienteClinico>.Fallo(
                    "No se pudo actualizar. Verifica que el expediente y el médico responsable existan.");
            }

            Auditar(usuarioId, terminal, "ActualizarExpediente", codigoSerologico);
            return ResultadoOperacion<ExpedienteClinico>.Ok(_expedientes.ObtenerPorCodigo(codigoSerologico));
        }

        // Ojo: en la auditoria va el codigo serologico pero NUNCA el
        // PacienteID. Si fueran juntos, la bitacora seria un segundo lugar
        // donde se une la identidad civil con el codigo (RF-CORE-02).
        private void Auditar(int usuarioId, string terminal, string accion, string codigoSerologico)
        {
            _auditoria.Registrar(usuarioId, null, terminal, accion, "expedientes_clinicos", null,
                "codigo=" + codigoSerologico);
        }

        private static string GenerarCodigo()
        {
            var bytes = new byte[LargoAleatorioCodigo];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            var letras = new char[LargoAleatorioCodigo];
            for (int i = 0; i < letras.Length; i++)
            {
                letras[i] = AlfabetoCodigo[bytes[i] % AlfabetoCodigo.Length];
            }
            return "EXP-" + new string(letras);
        }
    }
}
