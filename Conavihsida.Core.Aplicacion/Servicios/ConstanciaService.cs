using System;
using System.Collections.Generic;
using System.Linq;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Constancias de lo que el paciente hace en el Portal Web. El portal
    /// arma el contenido y aqui se guarda, porque el portal no tiene base
    /// de datos. Respalda /api/pacientes/{id}/constancias.
    ///
    /// El numero, la fecha y el nombre del paciente los pone siempre el
    /// CORE: lo que venga del portal en esos campos se ignora.
    /// </summary>
    public class ConstanciaService
    {
        public const string MensajePacienteNoExiste = "El paciente no existe.";

        private const int MaximoDetalles = 20;

        private readonly IConstanciaRepository _constancias;
        private readonly AuditoriaService _auditoria;

        public ConstanciaService(IConstanciaRepository constancias, AuditoriaService auditoria)
        {
            _constancias = constancias;
            _auditoria = auditoria;
        }

        public List<ConstanciaPaciente> ObtenerPorPaciente(int pacienteId)
        {
            return _constancias.ObtenerPorPaciente(pacienteId);
        }

        public ConstanciaPaciente Obtener(int pacienteId, int constanciaId)
        {
            return _constancias.Obtener(pacienteId, constanciaId);
        }

        public ResultadoOperacion<ConstanciaPaciente> Registrar(int pacienteId, ConstanciaPaciente datos, string terminal)
        {
            string tipo = Limpiar(datos.Tipo);
            string titulo = Limpiar(datos.Titulo);
            string descripcion = Limpiar(datos.Descripcion);

            if (tipo == null || tipo.Length > 40)
            {
                return Fallo("tipo es obligatorio y admite máximo 40 caracteres.");
            }

            if (titulo == null || titulo.Length > 150)
            {
                return Fallo("titulo es obligatorio y admite máximo 150 caracteres.");
            }

            if (descripcion != null && descripcion.Length > 300)
            {
                return Fallo("descripcion admite máximo 300 caracteres.");
            }

            // Las lineas sin etiqueta o sin valor se descartan.
            var detalles = (datos.Detalles ?? new List<DetalleConstancia>())
                .Where(d => d != null && Limpiar(d.Etiqueta) != null && Limpiar(d.Valor) != null)
                .Select(d => new DetalleConstancia { Etiqueta = d.Etiqueta.Trim(), Valor = d.Valor.Trim() })
                .ToList();

            if (detalles.Count > MaximoDetalles)
            {
                return Fallo("Una constancia admite máximo 20 detalles.");
            }

            if (detalles.Any(d => d.Etiqueta.Length > 60 || d.Valor.Length > 200))
            {
                return Fallo("Cada detalle admite máximo 60 caracteres en la etiqueta y 200 en el valor.");
            }

            var guardada = _constancias.Insertar(new ConstanciaPaciente
            {
                PacienteId = pacienteId,
                Tipo = tipo,
                Titulo = titulo,
                Descripcion = descripcion,
                FechaEmision = DateTime.Now,
                ReferenciaId = datos.ReferenciaId,
                Detalles = detalles
            });

            if (guardada == null)
            {
                return Fallo(MensajePacienteNoExiste);
            }

            // En la bitacora queda el tipo, no el contenido.
            _auditoria.Registrar(null, pacienteId, terminal, "RegistrarConstancia", "constancias_paciente",
                guardada.Id, "tipo=" + tipo);
            return ResultadoOperacion<ConstanciaPaciente>.Ok(guardada);
        }

        private static ResultadoOperacion<ConstanciaPaciente> Fallo(string mensaje)
        {
            return ResultadoOperacion<ConstanciaPaciente>.Fallo(mensaje);
        }

        private static string Limpiar(string texto)
        {
            return string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
        }
    }
}
