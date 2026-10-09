using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class FacturaRepository : IFacturaRepository
    {
        private readonly string _connectionString;

        public FacturaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private const string SelectBase = @"
            SELECT f.FacturaID, f.PacienteID, f.Concepto, f.Fecha, f.Monto, f.EsSubsidiada, f.Estado,
                   c.NumeroRecibo, c.NumeroFactura, p.FechaPago
            FROM Facturas f
            LEFT JOIN Comprobantes c ON c.FacturaID = f.FacturaID
            LEFT JOIN Pagos p ON p.FacturaID = f.FacturaID";

        public List<Factura> ObtenerPorPaciente(int pacienteId)
        {
            string sql = SelectBase + " WHERE f.PacienteID = @PacienteID ORDER BY f.Fecha DESC;";

            var lista = new List<Factura>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(LeerFactura(reader));
                    }
                }
            }
            return lista;
        }

        public Factura ObtenerPorId(int pacienteId, int facturaId)
        {
            string sql = SelectBase + " WHERE f.PacienteID = @PacienteID AND f.FacturaID = @FacturaID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                cmd.Parameters.AddWithValue("@FacturaID", facturaId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return LeerFactura(reader);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Requiere haber corrido antes:
        /// CREATE SEQUENCE dbo.SeqComprobante AS INT START WITH 1 INCREMENT BY 1;
        /// (ver adenda_secuencia_comprobante.sql)
        /// </summary>
        public int SiguienteNumeroComprobante()
        {
            const string sql = "SELECT NEXT VALUE FOR dbo.SeqComprobante;";
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public bool RegistrarPagoConComprobante(Pago pago, Comprobante comprobante)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1) Marcar la factura como Pagada SOLO si seguia Pendiente.
                        //    Esto evita una condicion de carrera con dos pagos simultaneos
                        //    de la misma factura (RN-05: no dejar el saldo inconsistente).
                        const string sqlFactura = @"
                            UPDATE Facturas SET Estado = @Pagada
                            WHERE FacturaID = @FacturaID AND Estado = @Pendiente;";

                        using (var cmdFactura = new SqlCommand(sqlFactura, conn, tx))
                        {
                            cmdFactura.Parameters.AddWithValue("@Pagada", EstadosFactura.Pagada);
                            cmdFactura.Parameters.AddWithValue("@Pendiente", EstadosFactura.Pendiente);
                            cmdFactura.Parameters.AddWithValue("@FacturaID", pago.FacturaId);

                            int filas = cmdFactura.ExecuteNonQuery();
                            if (filas != 1)
                            {
                                tx.Rollback();
                                return false;
                            }
                        }

                        // 2) Registrar el pago.
                        const string sqlPago = @"
                            INSERT INTO Pagos (FacturaID, Monto, FormaPago, ReferenciaPasarela, CajeroID, FechaPago)
                            VALUES (@FacturaID, @Monto, @FormaPago, @ReferenciaPasarela, @CajeroID, @FechaPago);";

                        using (var cmdPago = new SqlCommand(sqlPago, conn, tx))
                        {
                            cmdPago.Parameters.AddWithValue("@FacturaID", pago.FacturaId);
                            cmdPago.Parameters.AddWithValue("@Monto", pago.Monto);
                            cmdPago.Parameters.AddWithValue("@FormaPago", pago.FormaPago);
                            cmdPago.Parameters.AddWithValue("@ReferenciaPasarela",
                                (object)pago.ReferenciaPasarela ?? DBNull.Value);
                            cmdPago.Parameters.AddWithValue("@CajeroID",
                                (object)pago.CajeroId ?? DBNull.Value);
                            cmdPago.Parameters.AddWithValue("@FechaPago", pago.FechaPago);
                            cmdPago.ExecuteNonQuery();
                        }

                        // 3) RN-05: el comprobante se genera en la MISMA transaccion que el pago.
                        const string sqlComprobante = @"
                            INSERT INTO Comprobantes (FacturaID, NumeroRecibo, NumeroFactura, FechaEmision)
                            VALUES (@FacturaID, @NumeroRecibo, @NumeroFactura, @FechaEmision);";

                        using (var cmdComprobante = new SqlCommand(sqlComprobante, conn, tx))
                        {
                            cmdComprobante.Parameters.AddWithValue("@FacturaID", comprobante.FacturaId);
                            cmdComprobante.Parameters.AddWithValue("@NumeroRecibo", comprobante.NumeroRecibo);
                            cmdComprobante.Parameters.AddWithValue("@NumeroFactura", comprobante.NumeroFactura);
                            cmdComprobante.Parameters.AddWithValue("@FechaEmision", comprobante.FechaEmision);
                            cmdComprobante.ExecuteNonQuery();
                        }

                        tx.Commit();
                        return true;
                    }
                    catch (Exception)
                    {
                        // Ante cualquier fallo (BD caida, violacion de FK, etc.) no dejamos
                        // nada a medias: ni factura pagada sin pago, ni pago sin comprobante.
                        tx.Rollback();
                        return false;
                    }
                }
            }
        }

        private static Factura LeerFactura(SqlDataReader reader)
        {
            int ordFechaPago = reader.GetOrdinal("FechaPago");
            int ordRecibo = reader.GetOrdinal("NumeroRecibo");
            int ordFactura = reader.GetOrdinal("NumeroFactura");

            return new Factura
            {
                Id = reader.GetInt32(reader.GetOrdinal("FacturaID")),
                PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteID")),
                Concepto = reader.GetString(reader.GetOrdinal("Concepto")),
                Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
                EsSubsidiada = reader.GetBoolean(reader.GetOrdinal("EsSubsidiada")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                NumeroRecibo = reader.IsDBNull(ordRecibo) ? null : reader.GetString(ordRecibo),
                NumeroFactura = reader.IsDBNull(ordFactura) ? null : reader.GetString(ordFactura),
                FechaPago = reader.IsDBNull(ordFechaPago) ? (DateTime?)null : reader.GetDateTime(ordFechaPago)
            };
        }
    }
}
