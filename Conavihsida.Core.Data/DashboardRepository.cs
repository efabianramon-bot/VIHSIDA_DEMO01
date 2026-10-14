using System;
using System.Collections.Generic;
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
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
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
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
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
                      WHERE EsSubsidiada = 1 AND Fecha >= @Desde AND Estado <> @Anulada) AS Subsidiadas,
                    (SELECT COUNT(*) FROM Facturas
                      WHERE EsSubsidiada = 1 AND Fecha >= @Desde AND Estado = @Pagada)  AS Descargadas,
                    (SELECT COUNT(DISTINCT RecetaID) FROM MovimientosInventario
                      WHERE TipoMovimiento = @Salida AND FechaMovimiento >= @Desde)     AS Entregas,
                    (SELECT ISNULL(SUM(Cantidad), 0) FROM MovimientosInventario
                      WHERE TipoMovimiento = @Salida AND FechaMovimiento >= @Desde)     AS Unidades,
                    (SELECT COUNT(DISTINCT m.RecetaID) FROM MovimientosInventario m
                      WHERE m.TipoMovimiento = @Salida AND m.FechaMovimiento >= @Desde
                        AND EXISTS (SELECT 1 FROM CajaTransacciones t
                                    WHERE t.NumeroReceta = m.RecetaID AND t.Estado = @Completada)) AS ConDescargo,
                    (SELECT COUNT(*) FROM CajaTransacciones
                      WHERE EsSubsidiado = 1 AND Estado = @Completada AND Fecha >= @Desde) AS SubsidiadasCaja,
                    (SELECT COUNT(*) FROM CajaTransacciones
                      WHERE EsSubsidiado = 0 AND Estado = @Completada AND Fecha >= @Desde) AS CobrosCaja,
                    (SELECT ISNULL(SUM(Monto), 0) FROM CajaTransacciones
                      WHERE EsSubsidiado = 0 AND Estado = @Completada AND Fecha >= @Desde) AS MontoCobrado,
                    (SELECT COUNT(*) FROM CajaTransacciones
                      WHERE Estado = @Anulada AND Fecha >= @Desde)                      AS AnuladasCaja;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Pagada", EstadosFactura.Pagada);
                cmd.Parameters.AddWithValue("@Salida", TiposMovimiento.Salida);
                cmd.Parameters.AddWithValue("@Completada", "Completada");
                cmd.Parameters.AddWithValue("@Anulada", "Anulada");
                cmd.Parameters.AddWithValue("@Desde", desde);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    reader.Read();
                    int subsidiadas = reader.GetInt32(0);
                    int descargadas = reader.GetInt32(1);
                    return new SubsidiosVista
                    {
                        EntregasArvConDescargo = reader.GetInt32(4),
                        EntregasSubsidiadasCaja = reader.GetInt32(5),
                        CobrosCaja = reader.GetInt32(6),
                        MontoCobradoCaja = reader.GetDecimal(7),
                        TransaccionesAnuladasCaja = reader.GetInt32(8),
                        FacturasSubsidiadas = subsidiadas,
                        FacturasDescargadas = descargadas,
                        FacturasPendientes = subsidiadas - descargadas,
                        EntregasArv = reader.GetInt32(2),
                        UnidadesArvEntregadas = reader.GetInt32(3)
                    };
                }
            }
        }

        public List<TurnoCajaVista> ObtenerTurnosCaja(DateTime desde)
        {
            const string sql = @"
                SELECT NumeroTurno, Caja, ISNULL(NULLIF(NombreCajero, ''), UsuarioCajero) AS Cajero,
                       FechaApertura, FechaCierre, BalanceInicial, SaldoEsperado, EfectivoDeclarado,
                       Estado, Observacion
                FROM CajaTurnos
                WHERE FechaApertura >= @Desde
                ORDER BY FechaApertura DESC, TurnoID DESC;";

            var lista = new List<TurnoCajaVista>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Desde", desde);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    while (reader.Read())
                    {
                        lista.Add(new TurnoCajaVista
                        {
                            NumeroTurno = reader.GetString(0),
                            Caja = reader.IsDBNull(1) ? null : reader.GetString(1),
                            Cajero = reader.GetString(2),
                            FechaApertura = reader.GetDateTime(3),
                            FechaCierre = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4),
                            BalanceInicial = reader.GetDecimal(5),
                            SaldoEsperado = reader.GetDecimal(6),
                            EfectivoDeclarado = reader.IsDBNull(7) ? (decimal?)null : reader.GetDecimal(7),
                            Estado = reader.GetString(8),
                            Observacion = reader.IsDBNull(9) ? null : reader.GetString(9)
                        });
                    }
                }
            }
            return lista;
        }

        public List<FondoVista> ObtenerFondosActivos()
        {
            const string sql = @"
                SELECT Codigo, Nombre, TipoFuente, Organismo, MontoAsignado, MontoUtilizado, FechaFin
                FROM CajaFondos
                WHERE Activo = 1
                ORDER BY MontoAsignado DESC, Codigo;";

            var lista = new List<FondoVista>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    while (reader.Read())
                    {
                        lista.Add(new FondoVista
                        {
                            Codigo = reader.GetString(0),
                            Nombre = reader.GetString(1),
                            TipoFuente = reader.GetString(2),
                            Organismo = reader.IsDBNull(3) ? null : reader.GetString(3),
                            MontoAsignado = reader.GetDecimal(4),
                            MontoUtilizado = reader.GetDecimal(5),
                            FechaFin = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6)
                        });
                    }
                }
            }
            return lista;
        }
    }
}
