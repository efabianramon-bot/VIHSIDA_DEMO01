using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

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

                conn.AbrirConLog();
                try
                {
                    cmd.ExecuteNonQueryConLog();
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

                conn.AbrirConLog();
                try
                {
                    return cmd.ExecuteNonQueryConLog() > 0;
                }
                catch (SqlException ex) when (ex.Number == SqlErrorViolacionForanea)
                {
                    return false;
                }
            }
        }

        public List<ExpedienteClinico> Listar()
        {
            const string sql = "SELECT " + Columnas + @"
                FROM ExpedientesClinicos ORDER BY FechaApertura DESC, CodigoSerologico;";

            var lista = new List<ExpedienteClinico>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    while (reader.Read())
                    {
                        lista.Add(Leer(reader));
                    }
                }
            }
            return lista;
        }

        public PacienteClinicoVista BuscarPaciente(string cedulaSoloDigitos, int? pacienteId)
        {
            // La cedula se compara sin guiones para que encuentre igual
            // "001-1234567-8" que "00112345678".
            const string sql = @"
                SELECT p.PacienteID, p.NombreCompleto, p.Cedula, p.FechaNacimiento,
                       e.CodigoSerologico, e.Estado AS EstadoExpediente
                FROM Pacientes p
                LEFT JOIN ExpedientesClinicos e ON e.PacienteID = p.PacienteID
                WHERE (@PacienteID IS NOT NULL AND p.PacienteID = @PacienteID)
                   OR (@Cedula IS NOT NULL AND REPLACE(p.Cedula, '-', '') = @Cedula);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@PacienteID", SqlDbType.Int).Value = (object)pacienteId ?? DBNull.Value;
                cmd.Parameters.Add("@Cedula", SqlDbType.VarChar, 20).Value = (object)cedulaSoloDigitos ?? DBNull.Value;

                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new PacienteClinicoVista
                    {
                        PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteID")),
                        NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto")),
                        Cedula = LeerTexto(reader, "Cedula"),
                        FechaNacimiento = reader.IsDBNull(reader.GetOrdinal("FechaNacimiento"))
                            ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("FechaNacimiento")),
                        CodigoSerologico = LeerTexto(reader, "CodigoSerologico"),
                        EstadoExpediente = LeerTexto(reader, "EstadoExpediente")
                    };
                }
            }
        }

        private static ExpedienteClinico LeerUno(SqlConnection conn, SqlCommand cmd)
        {
            conn.AbrirConLog();
            using (var reader = cmd.ExecuteReaderConLog())
            {
                return reader.Read() ? Leer(reader) : null;
            }
        }

        private static ExpedienteClinico Leer(SqlDataReader reader)
        {
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

        private static string LeerTexto(SqlDataReader reader, string columna)
        {
            int ordinal = reader.GetOrdinal(columna);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }
    }
}
