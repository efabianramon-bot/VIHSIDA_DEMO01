using System;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Data
{
    public class DashboardRepository : IDashboardRepository
    {
        private readonly string _connectionString;

        public DashboardRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public PacientesActivosVista ContarPacientes(DateTime desde)
        {
            const string sql = @"
                SELECT
                    ISNULL(SUM(CASE WHEN Estado = @Activo THEN 1 ELSE 0 END), 0)      AS Activos,
                    ISNULL(SUM(CASE WHEN Estado = @Inactivo THEN 1 ELSE 0 END), 0)    AS Inactivos,
                    ISNULL(SUM(CASE WHEN Estado = @Transferido THEN 1 ELSE 0 END), 0) AS Transferidos,
                    COUNT(*)                                                           AS Total,
                    ISNULL(SUM(CASE WHEN FechaApertura >= @Desde THEN 1 ELSE 0 END), 0) AS Nuevos
                FROM ExpedientesClinicos;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Activo", EstadosExpediente.Activo);
                cmd.Parameters.AddWithValue("@Inactivo", EstadosExpediente.Inactivo);
                cmd.Parameters.AddWithValue("@Transferido", EstadosExpediente.Transferido);
                cmd.Parameters.AddWithValue("@Desde", desde);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return new PacientesActivosVista
                    {
                        Activos = reader.GetInt32(0),
                        Inactivos = reader.GetInt32(1),
                        Transferidos = reader.GetInt32(2),
                        Total = reader.GetInt32(3),
                        NuevosEnPeriodo = reader.GetInt32(4)
                    };
                }
            }
        }

        public AdherenciaVista ContarRecetas(DateTime desde)
        {
            const string sql = @"
                SELECT
                    COUNT(*)                                                           AS Emitidas,
                    ISNULL(SUM(CASE WHEN Estado = @Dispensada THEN 1 ELSE 0 END), 0)  AS Dispensadas,
                    ISNULL(SUM(CASE WHEN Estado = @Activa THEN 1 ELSE 0 END), 0)      AS Pendientes,
                    ISNULL(SUM(CASE WHEN Estado = @Vencida THEN 1 ELSE 0 END), 0)     AS Vencidas,
                    ISNULL(SUM(CASE WHEN Estado = @Cancelada THEN 1 ELSE 0 END), 0)   AS Canceladas,
                    COUNT(DISTINCT CodigoSerologico)                                   AS PacientesConReceta,
                    COUNT(DISTINCT CASE WHEN Estado = @Dispensada THEN CodigoSerologico END) AS PacientesConRetiro
                FROM Recetas
                WHERE FechaEmision >= @Desde;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Dispensada", EstadosReceta.Dispensada);
                cmd.Parameters.AddWithValue("@Activa", EstadosReceta.Activa);
                cmd.Parameters.AddWithValue("@Vencida", EstadosReceta.Vencida);
                cmd.Parameters.AddWithValue("@Cancelada", EstadosReceta.Cancelada);
                cmd.Parameters.AddWithValue("@Desde", desde);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    return new AdherenciaVista
                    {
                        RecetasEmitidas = reader.GetInt32(0),
                        RecetasDispensadas = reader.GetInt32(1),
                        RecetasPendientes = reader.GetInt32(2),
                        RecetasVencidas = reader.GetInt32(3),
                        RecetasCanceladas = reader.GetInt32(4),
                        PacientesConReceta = reader.GetInt32(5),
                        PacientesConRetiro = reader.GetInt32(6)
                    };
                }
            }
        }

        public SubsidiosVista ContarSubsidios(DateTime desde)
        {
            const string sql = @"
                SELECT
                    (SELECT COUNT(*) FROM Facturas
                      WHERE EsSubsidiada = 1 AND Fecha >= @Desde)                       AS Subsidiadas,
                    (SELECT COUNT(*) FROM Facturas
                      WHERE EsSubsidiada = 1 AND Fecha >= @Desde AND Estado = @Pagada)  AS Descargadas,
                    (SELECT COUNT(DISTINCT RecetaID) FROM MovimientosInventario
                      WHERE TipoMovimiento = @Salida AND FechaMovimiento >= @Desde)     AS Entregas,
                    (SELECT ISNULL(SUM(Cantidad), 0) FROM MovimientosInventario
                      WHERE TipoMovimiento = @Salida AND FechaMovimiento >= @Desde)     AS Unidades;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Pagada", EstadosFactura.Pagada);
                cmd.Parameters.AddWithValue("@Salida", TiposMovimiento.Salida);
                cmd.Parameters.AddWithValue("@Desde", desde);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    reader.Read();
                    int subsidiadas = reader.GetInt32(0);
                    int descargadas = reader.GetInt32(1);
                    return new SubsidiosVista
                    {
                        FacturasSubsidiadas = subsidiadas,
                        FacturasDescargadas = descargadas,
                        FacturasPendientes = subsidiadas - descargadas,
                        EntregasArv = reader.GetInt32(2),
                        UnidadesArvEntregadas = reader.GetInt32(3)
                    };
                }
            }
        }
    }
}
