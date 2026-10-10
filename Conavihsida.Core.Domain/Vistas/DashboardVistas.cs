using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Vistas
{
    // RF-CORE-08: indicadores del Dashboard Ejecutivo. Son solo totales:
    // a proposito NO llevan nombres, cedulas, codigos serologicos ni
    // valores clinicos, porque el Administrador no tiene rol Medico (RN-01).

    public class PacientesActivosVista
    {
        public int Dias { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }
        public int Transferidos { get; set; }
        public int Total { get; set; }

        /// <summary>Expedientes abiertos en los ultimos Dias.</summary>
        public int NuevosEnPeriodo { get; set; }
    }

    public class AdherenciaVista
    {
        public int Dias { get; set; }
        public int RecetasEmitidas { get; set; }
        public int RecetasDispensadas { get; set; }

        /// <summary>Recetas aun Activas: emitidas y sin retirar.</summary>
        public int RecetasPendientes { get; set; }
        public int RecetasVencidas { get; set; }
        public int RecetasCanceladas { get; set; }

        /// <summary>
        /// Dispensadas / (Emitidas - Canceladas) * 100. null si no hubo
        /// recetas en el periodo.
        /// </summary>
        public decimal? PorcentajeAdherencia { get; set; }

        public int PacientesConReceta { get; set; }

        /// <summary>Pacientes que retiraron al menos una receta en el periodo.</summary>
        public int PacientesConRetiro { get; set; }
    }

    public class InventarioCriticoVista
    {
        /// <summary>Un lote cuenta como "por vencer" si caduca dentro de estos dias.</summary>
        public int DiasAviso { get; set; }
        public int LotesVencidosConExistencia { get; set; }
        public int LotesPorVencer { get; set; }
        public int LotesBajoMinimo { get; set; }

        /// <summary>Lotes que cumplen alguna de las tres condiciones.</summary>
        public List<LoteMedicamento> Lotes { get; set; }
    }

    public class SubsidiosVista
    {
        public int Dias { get; set; }
        public int FacturasSubsidiadas { get; set; }

        /// <summary>Subsidiadas ya descargadas en Caja (estado Pagada).</summary>
        public int FacturasDescargadas { get; set; }
        public int FacturasPendientes { get; set; }

        /// <summary>Descargadas / Subsidiadas * 100. null si no hubo facturas subsidiadas.</summary>
        public decimal? PorcentajeEjecucion { get; set; }

        /// <summary>Recetas ARV entregadas en Farmacia (RN-02: 100% subsidiadas).</summary>
        public int EntregasArv { get; set; }
        public int UnidadesArvEntregadas { get; set; }
    }

    public class DashboardVista
    {
        public PacientesActivosVista Pacientes { get; set; }
        public AdherenciaVista Adherencia { get; set; }
        public InventarioCriticoVista Inventario { get; set; }
        public SubsidiosVista Subsidios { get; set; }
    }
}
