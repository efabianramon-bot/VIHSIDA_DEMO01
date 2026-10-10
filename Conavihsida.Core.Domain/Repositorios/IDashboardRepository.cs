using System;
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
    }
}
