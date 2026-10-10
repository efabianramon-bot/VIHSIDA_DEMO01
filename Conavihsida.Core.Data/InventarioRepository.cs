using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class InventarioRepository : IInventarioRepository
    {
        // Numeros de error de SQL Server: violacion de UNIQUE y de clave foranea.
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;
        private const int SqlErrorViolacionForanea = 547;

        private readonly string _connectionString;

        public InventarioRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Medicamento> ObtenerMedicamentos()
        {
            const string sql = @"
                SELECT MedicamentoID, Nombre, Presentacion, UnidadMedida
                FROM Medicamentos
                ORDER BY Nombre, Presentacion;";

            var lista = new List<Medicamento>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int ordinalPresentacion = reader.GetOrdinal("Presentacion");
                        lista.Add(new Medicamento
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("MedicamentoID")),
                            Nombre = reader.GetString(reader.GetOrdinal("Nombre")),
                            Presentacion = reader.IsDBNull(ordinalPresentacion)
                                ? null : reader.GetString(ordinalPresentacion),
                            UnidadMedida = reader.GetString(reader.GetOrdinal("UnidadMedida"))
                        });
                    }
                }
            }
            return lista;
        }

        public List<LoteMedicamento> ObtenerLotes(int? medicamentoId)
        {
            const string sql = @"
                SELECT l.LoteID, l.MedicamentoID, l.NumeroLote, l.FechaCaducidad,
                       l.ExistenciaActual, l.ExistenciaMinima, l.ExistenciaMaxima,
                       m.Nombre AS MedicamentoNombre
                FROM LotesMedicamento l
                INNER JOIN Medicamentos m ON m.MedicamentoID = l.MedicamentoID
                WHERE @MedicamentoID IS NULL OR l.MedicamentoID = @MedicamentoID
                ORDER BY l.FechaCaducidad, l.LoteID;";

            var lista = new List<LoteMedicamento>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@MedicamentoID", SqlDbType.Int).Value =
                    (object)medicamentoId ?? DBNull.Value;
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int ordinalMaxima = reader.GetOrdinal("ExistenciaMaxima");
                        lista.Add(new LoteMedicamento
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("LoteID")),
                            MedicamentoId = reader.GetInt32(reader.GetOrdinal("MedicamentoID")),
                            NumeroLote = reader.GetString(reader.GetOrdinal("NumeroLote")),
                            FechaCaducidad = reader.GetDateTime(reader.GetOrdinal("FechaCaducidad")),
                            ExistenciaActual = reader.GetInt32(reader.GetOrdinal("ExistenciaActual")),
                            ExistenciaMinima = reader.GetInt32(reader.GetOrdinal("ExistenciaMinima")),
                            ExistenciaMaxima = reader.IsDBNull(ordinalMaxima)
                                ? (int?)null : reader.GetInt32(ordinalMaxima),
                            MedicamentoNombre = reader.GetString(reader.GetOrdinal("MedicamentoNombre"))
                        });
                    }
                }
            }
            return lista;
        }

        public bool RegistrarLote(LoteMedicamento lote, int usuarioId, DateTime fecha)
        {
            const string sqlLote = @"
                INSERT INTO LotesMedicamento
                    (MedicamentoID, NumeroLote, FechaCaducidad, ExistenciaActual,
                     ExistenciaMinima, ExistenciaMaxima)
                OUTPUT INSERTED.LoteID
                VALUES
                    (@MedicamentoID, @NumeroLote, @FechaCaducidad, @ExistenciaActual,
                     @ExistenciaMinima, @ExistenciaMaxima);";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sqlLote, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@MedicamentoID", lote.MedicamentoId);
                            cmd.Parameters.AddWithValue("@NumeroLote", lote.NumeroLote);
                            cmd.Parameters.Add("@FechaCaducidad", SqlDbType.Date).Value = lote.FechaCaducidad.Date;
                            cmd.Parameters.AddWithValue("@ExistenciaActual", lote.ExistenciaActual);
                            cmd.Parameters.AddWithValue("@ExistenciaMinima", lote.ExistenciaMinima);
                            cmd.Parameters.AddWithValue("@ExistenciaMaxima",
                                (object)lote.ExistenciaMaxima ?? DBNull.Value);
                            lote.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        InsertarMovimiento(conn, tx, new MovimientoInventario
                        {
                            LoteId = lote.Id,
                            TipoMovimiento = TiposMovimiento.Entrada,
                            Cantidad = lote.ExistenciaActual,
                            UsuarioId = usuarioId,
                            FechaMovimiento = fecha
                        });

                        tx.Commit();
                        return true;
                    }
                    catch (SqlException ex) when (ex.Number == SqlErrorViolacionUnique
                                                || ex.Number == SqlErrorViolacionUniqueConstraint
                                                || ex.Number == SqlErrorViolacionForanea)
                    {
                        tx.Rollback();
                        return false;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        public Receta ObtenerReceta(int recetaId)
        {
            const string sql = @"
                SELECT RecetaID, HistorialID, CodigoSerologico, MedicamentoID, EsquemaTARV,
                       CantidadPrescrita, FechaEmision, Estado
                FROM Recetas WHERE RecetaID = @RecetaID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@RecetaID", recetaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    int ordinalEsquema = reader.GetOrdinal("EsquemaTARV");
                    return new Receta
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("RecetaID")),
                        HistorialId = reader.GetInt32(reader.GetOrdinal("HistorialID")),
                        CodigoSerologico = reader.GetString(reader.GetOrdinal("CodigoSerologico")),
                        MedicamentoId = reader.GetInt32(reader.GetOrdinal("MedicamentoID")),
                        EsquemaTarv = reader.IsDBNull(ordinalEsquema) ? null : reader.GetString(ordinalEsquema),
                        CantidadPrescrita = reader.GetInt32(reader.GetOrdinal("CantidadPrescrita")),
                        FechaEmision = reader.GetDateTime(reader.GetOrdinal("FechaEmision")),
                        Estado = reader.GetString(reader.GetOrdinal("Estado"))
                    };
                }
            }
        }

        public ResultadoDespacho DespacharReceta(int recetaId, int usuarioId, DateTime fecha,
                                                 List<MovimientoInventario> movimientos)
        {
            // El WHERE Estado = Activa es el candado: si dos farmaceuticos
            // despachan la misma receta a la vez, solo uno actualiza la fila.
            const string sqlMarcar = @"
                UPDATE Recetas SET Estado = @Dispensada
                OUTPUT INSERTED.MedicamentoID, INSERTED.CantidadPrescrita
                WHERE RecetaID = @RecetaID AND Estado = @Activa;";

            const string sqlExiste = "SELECT COUNT(*) FROM Recetas WHERE RecetaID = @RecetaID;";

            // UPDLOCK reserva los lotes hasta el COMMIT para que otro despacho
            // no cuente el mismo stock. Primero el que caduca antes.
            const string sqlLotes = @"
                SELECT LoteID, ExistenciaActual
                FROM LotesMedicamento WITH (UPDLOCK, ROWLOCK)
                WHERE MedicamentoID = @MedicamentoID AND ExistenciaActual > 0
                  AND FechaCaducidad >= @Hoy
                ORDER BY FechaCaducidad, LoteID;";

            const string sqlDescontar = @"
                UPDATE LotesMedicamento SET ExistenciaActual = ExistenciaActual - @Cantidad
                WHERE LoteID = @LoteID;";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        int medicamentoId;
                        int pendiente;
                        using (var cmd = new SqlCommand(sqlMarcar, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Dispensada", EstadosReceta.Dispensada);
                            cmd.Parameters.AddWithValue("@Activa", EstadosReceta.Activa);
                            cmd.Parameters.AddWithValue("@RecetaID", recetaId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    reader.Close();
                                    bool existe;
                                    using (var cmdExiste = new SqlCommand(sqlExiste, conn, tx))
                                    {
                                        cmdExiste.Parameters.AddWithValue("@RecetaID", recetaId);
                                        existe = Convert.ToInt32(cmdExiste.ExecuteScalar()) > 0;
                                    }
                                    tx.Rollback();
                                    return existe ? ResultadoDespacho.RecetaNoActiva : ResultadoDespacho.RecetaNoExiste;
                                }
                                medicamentoId = reader.GetInt32(0);
                                pendiente = reader.GetInt32(1);
                            }
                        }

                        var disponibles = new List<KeyValuePair<int, int>>();
                        using (var cmd = new SqlCommand(sqlLotes, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@MedicamentoID", medicamentoId);
                            cmd.Parameters.Add("@Hoy", SqlDbType.Date).Value = fecha.Date;
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    disponibles.Add(new KeyValuePair<int, int>(reader.GetInt32(0), reader.GetInt32(1)));
                                }
                            }
                        }

                        var salidas = new List<MovimientoInventario>();
                        foreach (var lote in disponibles)
                        {
                            if (pendiente == 0)
                            {
                                break;
                            }

                            int cantidad = Math.Min(pendiente, lote.Value);
                            pendiente -= cantidad;
                            salidas.Add(new MovimientoInventario
                            {
                                LoteId = lote.Key,
                                TipoMovimiento = TiposMovimiento.Salida,
                                RecetaId = recetaId,
                                Cantidad = cantidad,
                                UsuarioId = usuarioId,
                                FechaMovimiento = fecha
                            });
                        }

                        if (pendiente > 0)
                        {
                            // Deshace tambien el cambio de estado de la receta.
                            tx.Rollback();
                            return ResultadoDespacho.StockInsuficiente;
                        }

                        foreach (var salida in salidas)
                        {
                            using (var cmd = new SqlCommand(sqlDescontar, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@Cantidad", salida.Cantidad);
                                cmd.Parameters.AddWithValue("@LoteID", salida.LoteId);
                                cmd.ExecuteNonQuery();
                            }
                            InsertarMovimiento(conn, tx, salida);
                        }

                        tx.Commit();
                        movimientos.AddRange(salidas);
                        return ResultadoDespacho.Despachada;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void InsertarMovimiento(SqlConnection conn, SqlTransaction tx, MovimientoInventario movimiento)
        {
            const string sql = @"
                INSERT INTO MovimientosInventario
                    (LoteID, TipoMovimiento, RecetaID, Cantidad, UsuarioID, FechaMovimiento)
                OUTPUT INSERTED.MovimientoID
                VALUES
                    (@LoteID, @TipoMovimiento, @RecetaID, @Cantidad, @UsuarioID, @FechaMovimiento);";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@LoteID", movimiento.LoteId);
                cmd.Parameters.AddWithValue("@TipoMovimiento", movimiento.TipoMovimiento);
                cmd.Parameters.AddWithValue("@RecetaID", (object)movimiento.RecetaId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Cantidad", movimiento.Cantidad);
                cmd.Parameters.AddWithValue("@UsuarioID", movimiento.UsuarioId);
                cmd.Parameters.AddWithValue("@FechaMovimiento", movimiento.FechaMovimiento);
                movimiento.Id = Convert.ToInt32(cmd.ExecuteScalar());
            }
        }
    }
}
