using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Domain.Repositorios
{
    /// <summary>
    /// Conteos para el Dashboard Ejecutivo. Cada metodo llena solo los
    /// totales; los porcentajes los calcula DashboardService.
    /// </summary>
    public interface IDashboardRepository
    {
        PacientesActivosVista ContarPacientes(DateTime desde);

        AdherenciaVista ContarRecetas(DateTime desde);

        SubsidiosVista ContarSubsidios(DateTime desde);

        /// <summary>Fondos y convenios activos sincronizados por Caja, el de mayor monto primero.</summary>
        List<FondoVista> ObtenerFondosActivos();

        /// <summary>
        /// Turnos de caja abiertos desde esa fecha, el mas reciente primero.
        /// No calcula Diferencia: lo hace DashboardService.
        /// </summary>
        List<TurnoCajaVista> ObtenerTurnosCaja(DateTime desde);
    }
}
