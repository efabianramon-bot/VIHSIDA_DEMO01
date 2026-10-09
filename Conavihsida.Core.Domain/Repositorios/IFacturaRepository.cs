using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IFacturaRepository
    {
        List<Factura> ObtenerPorPaciente(int pacienteId);

        /// <summary>null si no existe o no pertenece a ese paciente.</summary>
        Factura ObtenerPorId(int pacienteId, int facturaId);

        /// <summary>Numero correlativo unico (ideal: SEQUENCE de la base de datos).</summary>
        int SiguienteNumeroComprobante();

        /// <summary>
        /// RN-05. Dentro de UNA sola transaccion: inserta el pago, inserta
        /// el comprobante y marca la factura como Pagada (solo si sigue
        /// Pendiente). Devuelve false, sin dejar nada a medias, si la
        /// factura ya no estaba Pendiente o si algo falla.
        /// </summary>
        bool RegistrarPagoConComprobante(Pago pago, Comprobante comprobante);
    }
}
