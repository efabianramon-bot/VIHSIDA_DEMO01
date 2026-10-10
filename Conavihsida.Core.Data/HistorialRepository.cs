using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class HistorialRepository : IHistorialRepository
    {
        // Numero de error de SQL Server para violacion de clave foranea.
        private const int SqlErrorViolacionForanea = 547;

        private readonly string _connectionString;

        public HistorialRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<HistorialClinico> ObtenerPorExpediente(string codigoSerologico)
        {
            const string sqlConsultas = @"
                SELECT HistorialID, CodigoSerologico, UsuarioMedicoID, FechaConsulta,
                       CargaViral, ConteoCD4, DiagnosticoDetalle, NotasClinicas
                FROM HistorialClinico
                WHERE CodigoSerologico = @Codigo
                ORDER BY FechaConsulta DESC, HistorialID DESC;";

            const string sqlRecetas = @"
                SELECT RecetaID, HistorialID, CodigoSerologico, MedicamentoID, EsquemaTARV,
                       CantidadPrescrita, FechaEmision, Estado
                FROM Recetas
                WHERE CodigoSerologico = @Codigo
                ORDER BY RecetaID;";

            var consultas = new List<HistorialClinico>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();

                using (var cmd = new SqlCommand(sqlConsultas, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigoSerologico);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            consultas.Add(new HistorialClinico
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("HistorialID")),
                                CodigoSerologico = reader.GetString(reader.GetOrdinal("CodigoSerologico")),
                                UsuarioMedicoId = reader.GetInt32(reader.GetOrdinal("UsuarioMedicoID")),
                                FechaConsulta = reader.GetDateTime(reader.GetOrdinal("FechaConsulta")),
                                CargaViral = reader.IsDBNull(reader.GetOrdinal("CargaViral"))
                                    ? (decimal?)null : reader.GetDecimal(reader.GetOrdinal("CargaViral")),
                                ConteoCd4 = reader.IsDBNull(reader.GetOrdinal("ConteoCD4"))
                                    ? (int?)null : reader.GetInt32(reader.GetOrdinal("ConteoCD4")),
                                DiagnosticoDetalle = LeerTexto(reader, "DiagnosticoDetalle"),
                                NotasClinicas = LeerTexto(reader, "NotasClinicas"),
                                Recetas = new List<Receta>()
                            });
                        }
                    }
                }

                var porId = consultas.ToDictionary(c => c.Id);
                using (var cmd = new SqlCommand(sqlRecetas, conn))
                {
                    cmd.Parameters.AddWithValue("@Codigo", codigoSerologico);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var receta = new Receta
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("RecetaID")),
                                HistorialId = reader.GetInt32(reader.GetOrdinal("HistorialID")),
                                CodigoSerologico = reader.GetString(reader.GetOrdinal("CodigoSerologico")),
                                MedicamentoId = reader.GetInt32(reader.GetOrdinal("MedicamentoID")),
                                EsquemaTarv = LeerTexto(reader, "EsquemaTARV"),
                                CantidadPrescrita = reader.GetInt32(reader.GetOrdinal("CantidadPrescrita")),
                                FechaEmision = reader.GetDateTime(reader.GetOrdinal("FechaEmision")),
                                Estado = reader.GetString(reader.GetOrdinal("Estado"))
                            };

                            HistorialClinico consulta;
                            if (porId.TryGetValue(receta.HistorialId, out consulta))
                            {
                                consulta.Recetas.Add(receta);
                            }
                        }
                    }
                }
            }
            return consultas;
        }

        public bool RegistrarConsulta(HistorialClinico consulta)
        {
            const string sqlConsulta = @"
                INSERT INTO HistorialClinico
                    (CodigoSerologico, UsuarioMedicoID, FechaConsulta, CargaViral, ConteoCD4,
                     DiagnosticoDetalle, NotasClinicas)
                OUTPUT INSERTED.HistorialID
                VALUES
                    (@Codigo, @UsuarioMedicoID, @FechaConsulta, @CargaViral, @ConteoCD4,
                     @DiagnosticoDetalle, @NotasClinicas);";

            const string sqlReceta = @"
                INSERT INTO Recetas
                    (HistorialID, CodigoSerologico, MedicamentoID, EsquemaTARV, CantidadPrescrita,
                     FechaEmision, Estado)
                OUTPUT INSERTED.RecetaID
                VALUES
                    (@HistorialID, @Codigo, @MedicamentoID, @EsquemaTARV, @CantidadPrescrita,
                     @FechaEmision, @Estado);";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sqlConsulta, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Codigo", consulta.CodigoSerologico);
                            cmd.Parameters.AddWithValue("@UsuarioMedicoID", consulta.UsuarioMedicoId);
                            cmd.Parameters.AddWithValue("@FechaConsulta", consulta.FechaConsulta);
                            cmd.Parameters.AddWithValue("@CargaViral", (object)consulta.CargaViral ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ConteoCD4", (object)consulta.ConteoCd4 ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@DiagnosticoDetalle",
                                (object)consulta.DiagnosticoDetalle ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@NotasClinicas",
                                (object)consulta.NotasClinicas ?? DBNull.Value);
                            consulta.Id = Convert.ToInt32(cmd.ExecuteScalar());
                        }

                        foreach (var receta in consulta.Recetas)
                        {
                            using (var cmd = new SqlCommand(sqlReceta, conn, tx))
                            {
                                // La receta siempre hereda el codigo de su consulta:
                                // asi las dos columnas no pueden contradecirse.
                                receta.HistorialId = consulta.Id;
                                receta.CodigoSerologico = consulta.CodigoSerologico;

                                cmd.Parameters.AddWithValue("@HistorialID", receta.HistorialId);
                                cmd.Parameters.AddWithValue("@Codigo", receta.CodigoSerologico);
                                cmd.Parameters.AddWithValue("@MedicamentoID", receta.MedicamentoId);
                                cmd.Parameters.AddWithValue("@EsquemaTARV",
                                    (object)receta.EsquemaTarv ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@CantidadPrescrita", receta.CantidadPrescrita);
                                cmd.Parameters.AddWithValue("@FechaEmision", receta.FechaEmision);
                                cmd.Parameters.AddWithValue("@Estado", receta.Estado);
                                receta.Id = Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                    catch (SqlException ex) when (ex.Number == SqlErrorViolacionForanea)
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

        private static string LeerTexto(SqlDataReader reader, string columna)
        {
            int ordinal = reader.GetOrdinal(columna);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
