using System;
using System.Collections.Generic;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Constancia de algo que el paciente hizo en el Portal Web (crear la
    /// cuenta, agendar o cancelar una cita, pagar una factura). El portal
    /// arma el contenido y el CORE lo guarda. Tablas: ConstanciasPaciente
    /// y ConstanciaDetalles.
    ///
    /// Los nombres de las propiedades son los del contrato del Portal Web
    /// (ConstanciaDto): no cambiarlos sin acordarlo. Nunca debe llevar
    /// informacion clinica (RN-01).
    /// </summary>
    public class ConstanciaPaciente
    {
        public int Id { get; set; }

        /// <summary>Lo genera el CORE: CON-AAAAMMDD-NNNN.</summary>
        public string Numero { get; set; }
        public int PacienteId { get; set; }

        /// <summary>Lo pone el CORE con el nombre registrado del paciente.</summary>
        public string NombrePaciente { get; set; }

        /// <summary>RegistroCuenta, CitaAgendada, CitaCancelada, PagoRealizado...</summary>
        public string Tipo { get; set; }
        public string Titulo { get; set; }
        public string Descripcion { get; set; }

        /// <summary>La pone el CORE al guardar.</summary>
        public DateTime FechaEmision { get; set; }

        /// <summary>Id de la cita o la factura a la que se refiere, si aplica.</summary>
        public int? ReferenciaId { get; set; }
        public List<DetalleConstancia> Detalles { get; set; }
    }

    public class DetalleConstancia
    {
        public string Etiqueta { get; set; }
        public string Valor { get; set; }
    }
}
