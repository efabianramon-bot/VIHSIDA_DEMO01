using System;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    /// <summary>
    /// Guarda lo que sincroniza Caja. Cada metodo es idempotente: si el
    /// registro ya existe (por su numero de negocio) lo actualiza en vez de
    /// duplicarlo, porque Caja reenvia lo que no sabe si llego y vuelve a
    /// mandar un turno al cerrarlo o una transaccion al anularla.
    /// Devuelven true si el registro era nuevo y false si ya existia.
    /// </summary>
    public interface ICajaRepository
    {
        bool GuardarTurno(TurnoCaja turno, DateTime ahora);

        bool GuardarMovimiento(MovimientoCaja movimiento, DateTime ahora);

        /// <summary>
        /// transaccion.CedulaPaciente debe venir con el formato
        /// 000-0000000-0; con ella se busca el PacienteID (queda NULL si
        /// no hay un paciente con esa cedula). Devuelve null, sin guardar
        /// nada, si NumeroReceta no es una receta de ese mismo paciente:
        /// de ese enlace depende que Farmacia vea la receta como descargada.
        /// </summary>
        bool? GuardarTransaccion(TransaccionCaja transaccion, DateTime ahora);

        bool GuardarComprobante(ComprobanteCaja comprobante, DateTime ahora);

        bool GuardarFondo(FondoConvenio fondo, DateTime ahora);
    }
}
