using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public enum ResultadoDespacho
    {
        Despachada,
        RecetaNoExiste,
        RecetaNoActiva,
        StockInsuficiente
    }

    public interface IInventarioRepository
    {
        List<Medicamento> ObtenerMedicamentos();

        /// <summary>Lotes ordenados por fecha de caducidad. medicamentoId null = todos.</summary>
        List<LoteMedicamento> ObtenerLotes(int? medicamentoId);

        /// <summary>
        /// Inserta el lote y su movimiento de Entrada en una transaccion y
        /// llena lote.Id. Debe devolver false (sin lanzar excepcion) si el
        /// medicamento no existe o ya tiene un lote con ese numero.
        /// </summary>
        bool RegistrarLote(LoteMedicamento lote, int usuarioId, DateTime fecha);

        /// <summary>null si no existe.</summary>
        Receta ObtenerReceta(int recetaId);

        /// <summary>Recetas en ese estado (ver EstadosReceta), la mas antigua primero.</summary>
        List<Receta> ObtenerRecetasPorEstado(string estado);

        /// <summary>
        /// Movimientos de Salida con los que se despacho esa receta, con el
        /// numero de lote y el nombre de quien despacho. Vacia si la receta
        /// no se ha despachado.
        /// </summary>
        List<MovimientoInventario> ObtenerSalidasDeReceta(int recetaId);

        /// <summary>
        /// Pasa a Vencida las recetas Activas emitidas antes de esa fecha.
        /// Devuelve cuantas cambio.
        /// </summary>
        int VencerRecetas(DateTime emitidasAntesDe);

        /// <summary>
        /// RF-CORE-04: despacha la receta completa en UNA transaccion. Solo
        /// si esta Activa: la marca Dispensada, descuenta el stock de los
        /// lotes no vencidos que caducan primero y deja un movimiento de
        /// Salida por cada lote usado. Si algo no se cumple, no cambia nada.
        /// </summary>
        ResultadoDespacho DespacharReceta(int recetaId, int usuarioId, DateTime fecha,
                                          List<MovimientoInventario> movimientos);
    }
}
