using System;
using System.Linq;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// RF-CORE-08: datos del Dashboard Ejecutivo. Respalda /api/dashboard,
    /// de uso exclusivo del rol Administrador. Solo devuelve totales.
    /// </summary>
    public class DashboardService
    {
        private const int DiasPorDefecto = 90;
        private const int DiasMaximo = 365;
        private const int DiasAvisoPorDefecto = 60;

        private readonly IDashboardRepository _dashboard;
        private readonly IInventarioRepository _inventario;

        public DashboardService(IDashboardRepository dashboard, IInventarioRepository inventario)
        {
            _dashboard = dashboard;
            _inventario = inventario;
        }

        public DashboardVista ObtenerResumen(int? dias, int? diasAviso)
        {
            return new DashboardVista
            {
                Pacientes = ObtenerPacientesActivos(dias),
                Adherencia = ObtenerAdherencia(dias),
                Inventario = ObtenerInventarioCritico(diasAviso),
                Subsidios = ObtenerSubsidios(dias)
            };
        }

        public PacientesActivosVista ObtenerPacientesActivos(int? dias)
        {
            int periodo = Ajustar(dias, DiasPorDefecto);
            var vista = _dashboard.ContarPacientes(Desde(periodo));
            vista.Dias = periodo;
            return vista;
        }

        public AdherenciaVista ObtenerAdherencia(int? dias)
        {
            int periodo = Ajustar(dias, DiasPorDefecto);
            var vista = _dashboard.ContarRecetas(Desde(periodo));
            vista.Dias = periodo;

            // Una receta cancelada por el medico no cuenta contra el paciente.
            vista.PorcentajeAdherencia = Porcentaje(
                vista.RecetasDispensadas, vista.RecetasEmitidas - vista.RecetasCanceladas);
            return vista;
        }

        public InventarioCriticoVista ObtenerInventarioCritico(int? diasAviso)
        {
            int aviso = Ajustar(diasAviso, DiasAvisoPorDefecto);
            var lotes = _inventario.ObtenerLotes(null);

            // Un lote vencido sin existencia ya no es un problema: no hay
            // nada que retirar. Uno agotado y vigente si (hay que reponer).
            var vencidos = lotes.Where(l => l.Vencido && l.ExistenciaActual > 0).ToList();
            var porVencer = lotes.Where(l => !l.Vencido && l.ExistenciaActual > 0
                                             && l.DiasParaCaducar <= aviso).ToList();
            var bajoMinimo = lotes.Where(l => !l.Vencido && l.BajoMinimo).ToList();

            return new InventarioCriticoVista
            {
                DiasAviso = aviso,
                LotesVencidosConExistencia = vencidos.Count,
                LotesPorVencer = porVencer.Count,
                LotesBajoMinimo = bajoMinimo.Count,
                Lotes = vencidos.Union(porVencer).Union(bajoMinimo)
                    .OrderBy(l => l.FechaCaducidad).ThenBy(l => l.Id).ToList()
            };
        }

        public SubsidiosVista ObtenerSubsidios(int? dias)
        {
            int periodo = Ajustar(dias, DiasPorDefecto);
            var vista = _dashboard.ContarSubsidios(Desde(periodo));
            vista.Dias = periodo;
            vista.PorcentajeEjecucion = Porcentaje(vista.FacturasDescargadas, vista.FacturasSubsidiadas);
            return vista;
        }

        private static int Ajustar(int? dias, int porDefecto)
        {
            return Math.Min(Math.Max(dias ?? porDefecto, 1), DiasMaximo);
        }

        private static DateTime Desde(int dias)
        {
            return DateTime.Today.AddDays(-dias);
        }

        private static decimal? Porcentaje(int parte, int total)
        {
            return total <= 0 ? (decimal?)null : Math.Round(parte * 100m / total, 1);
        }
    }
}
