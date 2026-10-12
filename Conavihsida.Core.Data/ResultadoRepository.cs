using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Data
{
    public class ResultadoRepository : IResultadoRepository
    {
        private readonly string _connectionString;

        public ResultadoRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<ResultadoPacienteVista> ObtenerResultadosPaciente(int pacienteId)
        {
            // RN-01: se lee de la VISTA, nunca de la tabla ResultadosLab.
            // vw_ResultadosPaciente no expone ValorResultado (dato clinico).
            const string sql = @"
                SELECT Id, PacienteID, Prueba, FechaSolicitud, Estado, Nota
                FROM vw_ResultadosPaciente
                WHERE PacienteID = @PacienteID
                ORDER BY FechaSolicitud DESC;";

            var lista = new List<ResultadoPacienteVista>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    int ordNota = reader.GetOrdinal("Nota");
                    while (reader.Read())
                    {
                        lista.Add(new ResultadoPacienteVista
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteID")),
                            Prueba = reader.GetString(reader.GetOrdinal("Prueba")),
                            FechaSolicitud = reader.GetDateTime(reader.GetOrdinal("FechaSolicitud")),
                            Estado = reader.GetString(reader.GetOrdinal("Estado")),
                            Nota = reader.IsDBNull(ordNota) ? null : reader.GetString(ordNota)
                        });
                    }
                }
            }
            return lista;
        }

        // ----- Lado clinico: aqui si se lee la tabla, con ValorResultado.
        // Estos metodos solo los usa LaboratorioService (rol Medico). -----

        private const string Columnas = @"ResultadoID, CodigoSerologico, Prueba, FechaSolicitud, Estado,
                       ValorResultado, FechaResultado, NotaPaciente";

        public List<ResultadoLab> ObtenerPorExpediente(string codigoSerologico)
        {
            const string sql = @"
                SELECT " + Columnas + @"
                FROM ResultadosLab
                WHERE CodigoSerologico = @Codigo
                ORDER BY FechaSolicitud DESC, ResultadoID DESC;";

            var lista = new List<ResultadoLab>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Codigo", codigoSerologico);
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

        public ResultadoLab ObtenerPorId(int resultadoId)
        {
            const string sql = @"
                SELECT " + Columnas + @"
                FROM ResultadosLab
                WHERE ResultadoID = @ResultadoID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ResultadoID", resultadoId);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    return reader.Read() ? Leer(reader) : null;
                }
            }
        }

        public int Insertar(ResultadoLab resultado)
        {
            const string sql = @"
                INSERT INTO ResultadosLab (CodigoSerologico, Prueba, FechaSolicitud, Estado, NotaPaciente)
                OUTPUT INSERTED.ResultadoID
                VALUES (@Codigo, @Prueba, @FechaSolicitud, @Estado, @NotaPaciente);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Codigo", resultado.CodigoSerologico);
                cmd.Parameters.AddWithValue("@Prueba", resultado.Prueba);
                cmd.Parameters.AddWithValue("@FechaSolicitud", resultado.FechaSolicitud);
                cmd.Parameters.AddWithValue("@Estado", resultado.Estado);
                cmd.Parameters.AddWithValue("@NotaPaciente", (object)resultado.NotaPaciente ?? DBNull.Value);
                conn.AbrirConLog();
                return Convert.ToInt32(cmd.ExecuteScalarConLog());
            }
        }

        public bool Publicar(int resultadoId, string valorResultado, string notaPaciente, DateTime fechaResultado)
        {
            // El WHERE por estado evita que dos publicaciones simultaneas
            // se pisen: solo la primera encuentra la fila Pendiente.
            const string sql = @"
                UPDATE ResultadosLab
                SET Estado = @Listo,
                    ValorResultado = @Valor,
                    FechaResultado = @FechaResultado,
                    NotaPaciente = COALESCE(@NotaPaciente, NotaPaciente)
                WHERE ResultadoID = @ResultadoID AND Estado = @Pendiente;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Listo", EstadosResultado.Listo);
                cmd.Parameters.AddWithValue("@Pendiente", EstadosResultado.Pendiente);
                cmd.Parameters.AddWithValue("@Valor", valorResultado);
                cmd.Parameters.AddWithValue("@FechaResultado", fechaResultado);
                cmd.Parameters.AddWithValue("@NotaPaciente", (object)notaPaciente ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@ResultadoID", resultadoId);
                conn.AbrirConLog();
                return cmd.ExecuteNonQueryConLog() == 1;
            }
        }

        private static ResultadoLab Leer(SqlDataReader reader)
        {
            int ordValor = reader.GetOrdinal("ValorResultado");
            int ordFecha = reader.GetOrdinal("FechaResultado");
            int ordNota = reader.GetOrdinal("NotaPaciente");
            return new ResultadoLab
            {
                Id = reader.GetInt32(reader.GetOrdinal("ResultadoID")),
                CodigoSerologico = reader.GetString(reader.GetOrdinal("CodigoSerologico")),
                Prueba = reader.GetString(reader.GetOrdinal("Prueba")),
                FechaSolicitud = reader.GetDateTime(reader.GetOrdinal("FechaSolicitud")),
                Estado = reader.GetString(reader.GetOrdinal("Estado")),
                ValorResultado = reader.IsDBNull(ordValor) ? null : reader.GetString(ordValor),
                FechaResultado = reader.IsDBNull(ordFecha) ? (DateTime?)null : reader.GetDateTime(ordFecha),
                NotaPaciente = reader.IsDBNull(ordNota) ? null : reader.GetString(ordNota)
            };
        }
    }
}
