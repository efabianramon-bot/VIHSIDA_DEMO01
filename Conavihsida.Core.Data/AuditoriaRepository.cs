using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly string _connectionString;

        public AuditoriaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Registrar(RegistroAuditoria registro)
        {
            const string sql = @"
                INSERT INTO Auditoria
                    (UsuarioID, PacienteID, Terminal, Accion, EntidadAfectada, EntidadID, FechaHora, Detalle)
                VALUES
                    (@UsuarioID, @PacienteID, @Terminal, @Accion, @EntidadAfectada, @EntidadID, @FechaHora, @Detalle);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UsuarioID", (object)registro.UsuarioId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@PacienteID", (object)registro.PacienteId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Terminal", (object)registro.Terminal ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Accion", registro.Accion);
                cmd.Parameters.AddWithValue("@EntidadAfectada", registro.EntidadAfectada);
                cmd.Parameters.AddWithValue("@EntidadID", (object)registro.EntidadId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaHora", registro.FechaHora);
                cmd.Parameters.AddWithValue("@Detalle", (object)registro.Detalle ?? System.DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        public List<RegistroAuditoria> Consultar(DateTime? desde, DateTime? hasta, int? usuarioId,
                                                 string accion, int maximo)
        {
            const string sql = @"
                SELECT TOP (@Maximo) AuditoriaID, UsuarioID, PacienteID, Terminal, Accion,
                       EntidadAfectada, EntidadID, FechaHora, Detalle
                FROM Auditoria
                WHERE (@Desde IS NULL OR FechaHora >= @Desde)
                  AND (@Hasta IS NULL OR FechaHora < @Hasta)
                  AND (@UsuarioID IS NULL OR UsuarioID = @UsuarioID)
                  AND (@Accion IS NULL OR Accion = @Accion)
                ORDER BY FechaHora DESC, AuditoriaID DESC;";

            var lista = new List<RegistroAuditoria>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@Maximo", SqlDbType.Int).Value = maximo;
                cmd.Parameters.Add("@Desde", SqlDbType.DateTime).Value = (object)desde ?? DBNull.Value;
                cmd.Parameters.Add("@Hasta", SqlDbType.DateTime).Value = (object)hasta ?? DBNull.Value;
                cmd.Parameters.Add("@UsuarioID", SqlDbType.Int).Value = (object)usuarioId ?? DBNull.Value;
                cmd.Parameters.Add("@Accion", SqlDbType.VarChar, 100).Value = (object)accion ?? DBNull.Value;

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new RegistroAuditoria
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("AuditoriaID")),
                            UsuarioId = LeerEntero(reader, "UsuarioID"),
                            PacienteId = LeerEntero(reader, "PacienteID"),
                            Terminal = LeerTexto(reader, "Terminal"),
                            Accion = reader.GetString(reader.GetOrdinal("Accion")),
                            EntidadAfectada = reader.GetString(reader.GetOrdinal("EntidadAfectada")),
                            EntidadId = LeerEntero(reader, "EntidadID"),
                            FechaHora = reader.GetDateTime(reader.GetOrdinal("FechaHora")),
                            Detalle = LeerTexto(reader, "Detalle")
                        });
                    }
                }
            }
            return lista;
        }

        private static int? LeerEntero(SqlDataReader reader, string columna)
        {
            int ordinal = reader.GetOrdinal(columna);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        private static string LeerTexto(SqlDataReader reader, string columna)
        {
            int ordinal = reader.GetOrdinal(columna);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
