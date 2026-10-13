using System;
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

        // ----- Lo que sincroniza el modulo de Caja -----

        /// <summary>De las EntregasArv, cuantas tienen su descargo registrado en Caja.</summary>
        public int EntregasArvConDescargo { get; set; }

        /// <summary>Transacciones subsidiadas Completadas en Caja en el periodo.</summary>
        public int EntregasSubsidiadasCaja { get; set; }

        /// <summary>Transacciones cobradas (no subsidiadas) Completadas en Caja.</summary>
        public int CobrosCaja { get; set; }
        public decimal MontoCobradoCaja { get; set; }
        public int TransaccionesAnuladasCaja { get; set; }

        /// <summary>Fondos y convenios activos que financian el subsidio.</summary>
        public List<FondoVista> Fondos { get; set; }
        public decimal MontoAsignadoFondos { get; set; }
        public decimal MontoUtilizadoFondos { get; set; }

        /// <summary>Utilizado / Asignado * 100 de los fondos activos. null si no hay fondos.</summary>
        public decimal? PorcentajeEjecucionFondos { get; set; }
    }

    /// <summary>Un fondo o convenio tal como lo reporta Caja. No lleva datos de pacientes.</summary>
    public class FondoVista
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string TipoFuente { get; set; }
        public string Organismo { get; set; }
        public decimal MontoAsignado { get; set; }
        public decimal MontoUtilizado { get; set; }
        public decimal? PorcentajeUtilizado { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    /// <summary>
    /// Cuadres de caja del periodo: cada turno que Caja sincronizo, con lo
    /// que debia haber en efectivo y lo que el cajero conto al cerrar.
    /// </summary>
    public class CuadresCajaVista
    {
        public int Dias { get; set; }
        public int TurnosCerrados { get; set; }
        public int TurnosAbiertos { get; set; }

        /// <summary>Cerrados con diferencia cero.</summary>
        public int Cuadrados { get; set; }
        public int ConFaltante { get; set; }
        public int ConSobrante { get; set; }

        /// <summary>Suma de los faltantes, como numero positivo.</summary>
        public decimal TotalFaltante { get; set; }
        public decimal TotalSobrante { get; set; }
        public List<TurnoCajaVista> Turnos { get; set; }
    }

    public class TurnoCajaVista
    {
        public string NumeroTurno { get; set; }
        public string Caja { get; set; }
        public string Cajero { get; set; }
        public DateTime FechaApertura { get; set; }
        public DateTime? FechaCierre { get; set; }
        public decimal BalanceInicial { get; set; }
        public decimal SaldoEsperado { get; set; }

        /// <summary>null mientras el turno siga abierto.</summary>
        public decimal? EfectivoDeclarado { get; set; }

        /// <summary>
        /// Declarado menos esperado: negativa = faltante, positiva =
        /// sobrante. null mientras el turno siga abierto.
        /// </summary>
        public decimal? Diferencia { get; set; }

        /// <summary>Abierto o Cerrado.</summary>
        public string Estado { get; set; }
        public string Observacion { get; set; }
    }

    public class DashboardVista
    {
        public PacientesActivosVista Pacientes { get; set; }
        public AdherenciaVista Adherencia { get; set; }
        public InventarioCriticoVista Inventario { get; set; }
        public SubsidiosVista Subsidios { get; set; }
    }
}
