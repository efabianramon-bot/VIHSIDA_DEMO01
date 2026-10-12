using System;
using System.Collections.Generic;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Lado clinico de los resultados de laboratorio: el medico solicita una
    /// prueba y luego publica su resultado. Respalda
    /// /api/clinica/expediente/{codigo}/resultados y /api/clinica/resultados/{id},
    /// de uso exclusivo del rol Medico (RN-01).
    ///
    /// El paciente ve estas mismas filas por ResultadoService, pero solo
    /// prueba, fecha, estado y la nota escrita para el: nunca el valor.
    /// </summary>
    public class LaboratorioService
    {
        public const string MensajeExpedienteNoExiste = "El expediente no existe.";
        public const string MensajeResultadoNoExiste = "El resultado no existe.";

        private const int LargoMaximoPrueba = 100;
        private const int LargoMaximoNota = 200;

        private readonly IResultadoRepository _resultados;
        private readonly IExpedienteRepository _expedientes;
        private readonly AuditoriaService _auditoria;

        public LaboratorioService(IResultadoRepository resultados, IExpedienteRepository expedientes,
                                  AuditoriaService auditoria)
        {
            _resultados = resultados;
            _expedientes = expedientes;
            _auditoria = auditoria;
        }

        /// <summary>null si el expediente no existe.</summary>
        public List<ResultadoLab> ObtenerResultados(string codigoSerologico, int usuarioId, string terminal)
        {
            if (_expedientes.ObtenerPorCodigo(codigoSerologico) == null)
            {
                return null;
            }

            var lista = _resultados.ObtenerPorExpediente(codigoSerologico);
            Auditar(usuarioId, terminal, "ConsultarResultadosClinicos", null, codigoSerologico);
            return lista;
        }

        /// <summary>Crea la solicitud en estado Pendiente; el paciente la ve de inmediato.</summary>
        public ResultadoOperacion<ResultadoLab> SolicitarPrueba(
            string codigoSerologico, string prueba, string notaPaciente, int usuarioId, string terminal)
        {
            prueba = Limpiar(prueba);
            notaPaciente = Limpiar(notaPaciente);

            if (prueba == null)
            {
                return Fallo("Indica qué prueba se solicita.");
            }

            if (prueba.Length > LargoMaximoPrueba)
            {
                return Fallo("El nombre de la prueba admite máximo 100 caracteres.");
            }

            if (notaPaciente != null && notaPaciente.Length > LargoMaximoNota)
            {
                return Fallo("La nota para el paciente admite máximo 200 caracteres.");
            }

            var expediente = _expedientes.ObtenerPorCodigo(codigoSerologico);
            if (expediente == null)
            {
                return Fallo(MensajeExpedienteNoExiste);
            }

            if (expediente.Estado != EstadosExpediente.Activo)
            {
                return Fallo("El expediente no está activo; no admite nuevas pruebas.");
            }

            var resultado = new ResultadoLab
            {
                CodigoSerologico = expediente.CodigoSerologico,
                Prueba = prueba,
                FechaSolicitud = DateTime.Now,
                Estado = EstadosResultado.Pendiente,
                NotaPaciente = notaPaciente
            };
            resultado.Id = _resultados.Insertar(resultado);

            Auditar(usuarioId, terminal, "SolicitarPrueba", resultado.Id, resultado.CodigoSerologico);
            return ResultadoOperacion<ResultadoLab>.Ok(resultado);
        }

        /// <summary>Registra el valor y deja el resultado Listo. Un resultado publicado no se modifica.</summary>
        public ResultadoOperacion<ResultadoLab> PublicarResultado(
            int resultadoId, string valorResultado, string notaPaciente, int usuarioId, string terminal)
        {
            valorResultado = Limpiar(valorResultado);
            notaPaciente = Limpiar(notaPaciente);

            if (valorResultado == null)
            {
                return Fallo("Indica el valor del resultado.");
            }

            if (notaPaciente != null && notaPaciente.Length > LargoMaximoNota)
            {
                return Fallo("La nota para el paciente admite máximo 200 caracteres.");
            }

            var resultado = _resultados.ObtenerPorId(resultadoId);
            if (resultado == null)
            {
                return Fallo(MensajeResultadoNoExiste);
            }

            var ahora = DateTime.Now;
            if (resultado.Estado == EstadosResultado.Listo
                || !_resultados.Publicar(resultadoId, valorResultado, notaPaciente, ahora))
            {
                return Fallo("Este resultado ya fue publicado.");
            }

            resultado.Estado = EstadosResultado.Listo;
            resultado.ValorResultado = valorResultado;
            resultado.FechaResultado = ahora;
            resultado.NotaPaciente = notaPaciente ?? resultado.NotaPaciente;

            // Ojo: en la auditoria NO va el valor ni la nota (RN-01).
            Auditar(usuarioId, terminal, "PublicarResultado", resultado.Id, resultado.CodigoSerologico);
            return ResultadoOperacion<ResultadoLab>.Ok(resultado);
        }

        private void Auditar(int usuarioId, string terminal, string accion, int? entidadId, string codigoSerologico)
        {
            _auditoria.Registrar(usuarioId, null, terminal, accion, "resultados_lab", entidadId,
                "codigo=" + codigoSerologico);
        }

        private static ResultadoOperacion<ResultadoLab> Fallo(string mensaje)
        {
            return ResultadoOperacion<ResultadoLab>.Fallo(mensaje);
        }

        private static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
    }
}
