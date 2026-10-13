using System;
using System.Collections.Generic;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-04: inventario farmaceutico. Respalda /api/farmacia: lotes
    /// con caducidad y existencias minima/maxima, y despacho de recetas.
    /// La unica forma de sacar stock es despachando una receta Activa.
    /// </summary>
    public class InventarioService
    {
        public const string MensajeRecetaNoExiste = "La receta no existe.";

        private const int LargoMaximoNumeroLote = 50;

        private readonly IInventarioRepository _inventario;
        private readonly AuditoriaService _auditoria;

        public InventarioService(IInventarioRepository inventario, AuditoriaService auditoria)
        {
            _inventario = inventario;
            _auditoria = auditoria;
        }

        public List<Medicamento> ObtenerMedicamentos()
        {
            return _inventario.ObtenerMedicamentos();
        }

        public List<LoteMedicamento> ObtenerLotes(int? medicamentoId)
        {
            return _inventario.ObtenerLotes(medicamentoId);
        }

        /// <summary>
        /// Pasa a Vencida toda receta Activa con mas de
        /// ReglasReceta.DiasVigencia dias sin despacho. No hay un proceso
        /// nocturno: se llama justo antes de leer o despachar recetas, asi
        /// nadie ve como Activa una receta que ya vencio.
        /// </summary>
        public int VencerRecetasAntiguas(string terminal)
        {
            int vencidas = _inventario.VencerRecetas(DateTime.Now.AddDays(-ReglasReceta.DiasVigencia));
            if (vencidas > 0)
            {
                // Sin usuario: lo hace el sistema, no quien disparo la lectura.
                _auditoria.Registrar(null, null, terminal, "VencerRecetas", "recetas", null,
                    string.Format("cantidad={0};diasVigencia={1}", vencidas, ReglasReceta.DiasVigencia));
            }
            return vencidas;
        }

        public Receta ObtenerReceta(int recetaId, int usuarioId, string terminal)
        {
            VencerRecetasAntiguas(terminal);
            var receta = _inventario.ObtenerReceta(recetaId);
            if (receta != null)
            {
                _auditoria.Registrar(usuarioId, null, terminal, "ConsultarReceta", "recetas", recetaId, null);
            }
            return receta;
        }

        /// <summary>
        /// Con que lotes y quien despacho la receta, para el comprobante de
        /// despacho. null si la receta no existe; vacia si no se ha despachado.
        /// </summary>
        public List<MovimientoInventario> ObtenerDespacho(int recetaId, int usuarioId, string terminal)
        {
            if (_inventario.ObtenerReceta(recetaId) == null)
            {
                return null;
            }

            var salidas = _inventario.ObtenerSalidasDeReceta(recetaId);
            _auditoria.Registrar(usuarioId, null, terminal, "ConsultarDespacho", "recetas", recetaId, null);
            return salidas;
        }

        /// <summary>
        /// Recetas en un estado; sin estado, las Activas (las que esperan
        /// despacho). null si el estado no existe.
        /// </summary>
        public List<Receta> ListarRecetas(string estado, int usuarioId, string terminal)
        {
            estado = string.IsNullOrWhiteSpace(estado) ? EstadosReceta.Activa : estado.Trim();
            if (!EstadosReceta.EsValido(estado))
            {
                return null;
            }

            VencerRecetasAntiguas(terminal);
            var recetas = _inventario.ObtenerRecetasPorEstado(estado);
            _auditoria.Registrar(usuarioId, null, terminal, "ListarRecetas", "recetas", null, "estado=" + estado);
            return recetas;
        }

        public ResultadoOperacion<LoteMedicamento> RegistrarLote(
            int medicamentoId, string numeroLote, DateTime fechaCaducidad, int cantidad,
            int existenciaMinima, int? existenciaMaxima, int usuarioId, string terminal)
        {
            if (medicamentoId <= 0)
            {
                return Fallo("El medicamento es obligatorio.");
            }

            numeroLote = (numeroLote ?? string.Empty).Trim();
            if (numeroLote.Length == 0 || numeroLote.Length > LargoMaximoNumeroLote)
            {
                return Fallo("El número de lote es obligatorio y admite máximo 50 caracteres.");
            }

            if (fechaCaducidad.Date <= DateTime.Today)
            {
                return Fallo("No se puede registrar un lote vencido o que vence hoy.");
            }

            if (cantidad <= 0)
            {
                return Fallo("La cantidad recibida debe ser mayor que cero.");
            }

            if (existenciaMinima < 0)
            {
                return Fallo("La existencia mínima no puede ser negativa.");
            }

            if (existenciaMaxima != null && existenciaMaxima < existenciaMinima)
            {
                return Fallo("La existencia máxima no puede ser menor que la mínima.");
            }

            if (existenciaMaxima != null && cantidad > existenciaMaxima)
            {
                return Fallo("La cantidad recibida supera la existencia máxima del lote.");
            }

            var lote = new LoteMedicamento
            {
                MedicamentoId = medicamentoId,
                NumeroLote = numeroLote,
                FechaCaducidad = fechaCaducidad.Date,
                ExistenciaActual = cantidad,
                ExistenciaMinima = existenciaMinima,
                ExistenciaMaxima = existenciaMaxima
            };

            if (!_inventario.RegistrarLote(lote, usuarioId, DateTime.Now))
            {
                return Fallo("No se registró el lote. Verifica que el medicamento exista y que el número de lote no esté repetido.");
            }

            _auditoria.Registrar(usuarioId, null, terminal, "RegistrarLote", "lotes_medicamento", lote.Id,
                string.Format("medicamentoId={0};cantidad={1}", medicamentoId, cantidad));

            return ResultadoOperacion<LoteMedicamento>.Ok(lote);
        }

        /// <summary>Devuelve los movimientos de Salida, uno por cada lote del que se saco stock.</summary>
        public ResultadoOperacion<List<MovimientoInventario>> DespacharReceta(int recetaId, int usuarioId,
                                                                              string terminal)
        {
            VencerRecetasAntiguas(terminal);

            // Decision del equipo: la falta de descargo en Caja NO bloquea el
            // despacho (la terminal puede estar sin conexion y el descargo
            // llega al sincronizar), pero queda anotado en la bitacora.
            var receta = _inventario.ObtenerReceta(recetaId);
            bool descargada = receta != null && receta.DescargadaEnCaja;

            var movimientos = new List<MovimientoInventario>();
            var resultado = _inventario.DespacharReceta(recetaId, usuarioId, DateTime.Now, movimientos);

            switch (resultado)
            {
                case ResultadoDespacho.RecetaNoExiste:
                    return ResultadoOperacion<List<MovimientoInventario>>.Fallo(MensajeRecetaNoExiste);
                case ResultadoDespacho.RecetaNoActiva:
                    return ResultadoOperacion<List<MovimientoInventario>>.Fallo(
                        "La receta no está activa: ya fue dispensada, venció o fue cancelada.");
                case ResultadoDespacho.StockInsuficiente:
                    return ResultadoOperacion<List<MovimientoInventario>>.Fallo(
                        "No hay existencia suficiente en lotes vigentes para despachar la receta completa.");
            }

            int total = 0;
            foreach (var movimiento in movimientos)
            {
                total += movimiento.Cantidad;
            }

            _auditoria.Registrar(usuarioId, null, terminal, "DespacharReceta", "recetas", recetaId,
                string.Format("cantidad={0};lotes={1};descargoCaja={2}", total, movimientos.Count,
                    descargada ? "si" : "no"));

            if (!descargada)
            {
                // Accion aparte para que el auditor pueda listarlos de una vez.
                _auditoria.Registrar(usuarioId, null, terminal, "DespachoSinDescargo", "recetas", recetaId, null);
            }

            return ResultadoOperacion<List<MovimientoInventario>>.Ok(movimientos);
        }

        private static ResultadoOperacion<LoteMedicamento> Fallo(string mensaje)
        {
            return ResultadoOperacion<LoteMedicamento>.Fallo(mensaje);
        }
    }
}
