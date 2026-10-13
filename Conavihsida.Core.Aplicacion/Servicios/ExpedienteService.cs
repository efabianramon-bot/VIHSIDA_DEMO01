using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

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

        // La cedula dominicana tiene 11 digitos (000-0000000-0).
        private const int LargoCedula = 11;

        private static readonly System.Text.RegularExpressions.Regex FormatoCorreo =
            new System.Text.RegularExpressions.Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");

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

        public List<ExpedienteClinico> ListarExpedientes(int usuarioId, string terminal)
        {
            var lista = _expedientes.Listar();
            _auditoria.Registrar(usuarioId, null, terminal, "ListarExpedientes", "expedientes_clinicos", null, null);
            return lista;
        }

        /// <summary>
        /// Identifica a un paciente por cedula o por Id y dice si ya tiene
        /// expediente. Indica solo uno de los dos criterios. Devuelve null
        /// si el criterio no es valido o el paciente no existe.
        /// </summary>
        public PacienteClinicoVista BuscarPaciente(string cedula, int? pacienteId, int usuarioId, string terminal)
        {
            string digitos = SoloDigitos(cedula);
            bool porCedula = digitos.Length == LargoCedula;
            bool porId = pacienteId != null && pacienteId > 0;
            if (!porCedula && !porId)
            {
                return null;
            }

            var paciente = _expedientes.BuscarPaciente(porCedula ? digitos : null, porCedula ? null : pacienteId);
            if (paciente != null)
            {
                // Aqui va el PacienteID y NO el codigo serologico, por la
                // misma razon que en Auditar(): no juntarlos en la bitacora.
                _auditoria.Registrar(usuarioId, paciente.PacienteId, terminal, "IdentificarPaciente",
                    "pacientes", paciente.PacienteId, null);
            }
            return paciente;
        }

        /// <summary>
        /// Registra a un paciente que llega a consulta sin cuenta en el
        /// Portal Web, para poder abrirle expediente. Queda sin acceso a la
        /// web: lo obtiene cuando el mismo se registra con esta cedula y,
        /// si aqui se anoto un correo, con ese mismo correo.
        /// </summary>
        public ResultadoOperacion<PacienteClinicoVista> RegistrarPaciente(
            string nombreCompleto, string cedula, DateTime? fechaNacimiento, string correo,
            int usuarioId, string terminal)
        {
            nombreCompleto = (nombreCompleto ?? string.Empty).Trim();
            correo = string.IsNullOrWhiteSpace(correo) ? null : correo.Trim().ToLowerInvariant();
            string digitos = SoloDigitos(cedula);

            if (nombreCompleto.Length == 0 || nombreCompleto.Length > 150)
            {
                return ResultadoOperacion<PacienteClinicoVista>.Fallo(
                    "El nombre completo es obligatorio y admite máximo 150 caracteres.");
            }

            if (digitos.Length != LargoCedula)
            {
                return ResultadoOperacion<PacienteClinicoVista>.Fallo("La cédula debe tener 11 dígitos.");
            }

            if (correo != null && (correo.Length > 150 || !FormatoCorreo.IsMatch(correo)))
            {
                return ResultadoOperacion<PacienteClinicoVista>.Fallo("El correo electrónico no es válido.");
            }

            if (fechaNacimiento != null
                && (fechaNacimiento.Value.Date > DateTime.Today || fechaNacimiento.Value.Year < 1900))
            {
                return ResultadoOperacion<PacienteClinicoVista>.Fallo("La fecha de nacimiento no es válida.");
            }

            string cedulaConGuiones = digitos.Insert(10, "-").Insert(3, "-");
            int? id = _expedientes.RegistrarPaciente(nombreCompleto, cedulaConGuiones,
                fechaNacimiento == null ? (DateTime?)null : fechaNacimiento.Value.Date, correo, DateTime.Now);
            if (id == null)
            {
                return ResultadoOperacion<PacienteClinicoVista>.Fallo(
                    "Ya hay un paciente registrado con esa cédula o con ese correo.");
            }

            _auditoria.Registrar(usuarioId, id, terminal, "RegistrarPacientePorEmpleado", "pacientes", id, null);
            return ResultadoOperacion<PacienteClinicoVista>.Ok(_expedientes.BuscarPaciente(null, id));
        }

        public ResultadoOperacion<ExpedienteClinico> AbrirExpediente(int pacienteId, int? medicoResponsableId,
                                                                     int usuarioId, string terminal)
        {
            if (_expedientes.ObtenerPorPaciente(pacienteId) != null)
            {
                return ResultadoOperacion<ExpedienteClinico>.Fallo("El paciente ya tiene un expediente clínico.");
            }

            // Si no se indica otro, el responsable es el medico que lo abre.
            // Queda sin responsable solo si quien abre no tiene ficha de medico.
            var expediente = new ExpedienteClinico
            {
                PacienteId = pacienteId,
                MedicoResponsableId = medicoResponsableId ?? _expedientes.ObtenerMedicoIdPorUsuario(usuarioId),
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

        private static string SoloDigitos(string texto)
        {
            return new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
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
