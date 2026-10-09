using System.Collections.Generic;
using Microsoft.Data.SqlClient;
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
                conn.Open();
                using (var reader = cmd.ExecuteReader())
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
    }
}
