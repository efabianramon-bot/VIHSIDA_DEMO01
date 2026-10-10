using System;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class ExpedienteRepository : IExpedienteRepository
    {
        // Numeros de error de SQL Server: violacion de UNIQUE / PRIMARY KEY
        // y violacion de clave foranea.
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;
        private const int SqlErrorViolacionForanea = 547;

        private const string Columnas =
            "CodigoSerologico, PacienteID, MedicoResponsableID, FechaApertura, Estado";

        private readonly string _connectionString;

        public ExpedienteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ExpedienteClinico ObtenerPorCodigo(string codigoSerologico)
        {
            const string sql = "SELECT " + Columnas + @"
                FROM ExpedientesClinicos WHERE CodigoSerologico = @Codigo;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Codigo", codigoSerologico);
                return LeerUno(conn, cmd);
            }
        }

        public ExpedienteClinico ObtenerPorPaciente(int pacienteId)
        {
            const string sql = "SELECT " + Columnas + @"
                FROM ExpedientesClinicos WHERE PacienteID = @PacienteID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                return LeerUno(conn, cmd);
            }
        }

        public bool Insertar(ExpedienteClinico expediente)
        {
            const string sql = @"
                INSERT INTO ExpedientesClinicos
                    (CodigoSerologico, PacienteID, MedicoResponsableID, FechaApertura, Estado)
                VALUES
                    (@Codigo, @PacienteID, @MedicoResponsableID, @FechaApertura, @Estado);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Codigo", expediente.CodigoSerologico);
                cmd.Parameters.AddWithValue("@PacienteID", expediente.PacienteId);
                cmd.Parameters.AddWithValue("@MedicoResponsableID",
                    (object)expediente.MedicoResponsableId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaApertura", expediente.FechaApertura);
                cmd.Parameters.AddWithValue("@Estado", expediente.Estado);

                conn.Open();
                try
                {
                    cmd.ExecuteNonQuery();
                    return true;
                }
                catch (SqlException ex) when (ex.Number == SqlErrorViolacionUnique
                                            || ex.Number == SqlErrorViolacionUniqueConstraint
                                            || ex.Number == SqlErrorViolacionForanea)
                {
                    return false;
                }
            }
        }

        public bool Actualizar(string codigoSerologico, int? medicoResponsableId, string estado)
        {
            const string sql = @"
                UPDATE ExpedientesClinicos
                SET MedicoResponsableID = @MedicoResponsableID, Estado = @Estado
                WHERE CodigoSerologico = @Codigo;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MedicoResponsableID",
                    (object)medicoResponsableId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Estado", estado);
                cmd.Parameters.AddWithValue("@Codigo", codigoSerologico);

                conn.Open();
                try
                {
                    return cmd.ExecuteNonQuery() > 0;
                }
                catch (SqlException ex) when (ex.Number == SqlErrorViolacionForanea)
                {
                    return false;
                }
            }
        }

        private static ExpedienteClinico LeerUno(SqlConnection conn, SqlCommand cmd)
        {
            conn.Open();
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read())
                {
                    return null;
                }

                int ordinalMedico = reader.GetOrdinal("MedicoResponsableID");
                return new ExpedienteClinico
                {
                    CodigoSerologico = reader.GetString(reader.GetOrdinal("CodigoSerologico")),
                    PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteID")),
                    MedicoResponsableId = reader.IsDBNull(ordinalMedico)
                        ? (int?)null : reader.GetInt32(ordinalMedico),
                    FechaApertura = reader.GetDateTime(reader.GetOrdinal("FechaApertura")),
                    Estado = reader.GetString(reader.GetOrdinal("Estado"))
                };
            }
        }
    }
}
