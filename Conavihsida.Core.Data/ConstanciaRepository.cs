using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class ConstanciaRepository : IConstanciaRepository
    {
        // Numero de error de SQL Server para violacion de clave foranea.
        private const int SqlErrorViolacionForanea = 547;

        private const string Seleccion = @"
                SELECT c.ConstanciaID, c.Numero, c.PacienteID, p.NombreCompleto, c.Tipo, c.Titulo,
                       c.Descripcion, c.FechaEmision, c.ReferenciaID
                FROM ConstanciasPaciente c
                JOIN Pacientes p ON p.PacienteID = c.PacienteID
                WHERE c.PacienteID = @PacienteID";

        private readonly string _connectionString;

        public ConstanciaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ConstanciaPaciente Insertar(ConstanciaPaciente constancia)
        {
            const string sqlConstancia = @"
                INSERT INTO ConstanciasPaciente (PacienteID, Tipo, Titulo, Descripcion, FechaEmision, ReferenciaID)
                OUTPUT INSERTED.ConstanciaID
                VALUES (@PacienteID, @Tipo, @Titulo, @Descripcion, @FechaEmision, @ReferenciaID);";

            const string sqlDetalle = @"
                INSERT INTO ConstanciaDetalles (ConstanciaID, Orden, Etiqueta, Valor)
                VALUES (@ConstanciaID, @Orden, @Etiqueta, @Valor);";

            int id;
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.AbrirConLog();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (var cmd = new SqlCommand(sqlConstancia, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@PacienteID", constancia.PacienteId);
                            cmd.Parameters.AddWithValue("@Tipo", constancia.Tipo);
                            cmd.Parameters.AddWithValue("@Titulo", constancia.Titulo);
                            cmd.Parameters.AddWithValue("@Descripcion", (object)constancia.Descripcion ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@FechaEmision", constancia.FechaEmision);
                            cmd.Parameters.AddWithValue("@ReferenciaID", (object)constancia.ReferenciaId ?? DBNull.Value);
                            id = Convert.ToInt32(cmd.ExecuteScalarConLog());
                        }

                        for (int i = 0; i < constancia.Detalles.Count; i++)
                        {
                            using (var cmd = new SqlCommand(sqlDetalle, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@ConstanciaID", id);
                                cmd.Parameters.AddWithValue("@Orden", i + 1);
                                cmd.Parameters.AddWithValue("@Etiqueta", constancia.Detalles[i].Etiqueta);
                                cmd.Parameters.AddWithValue("@Valor", constancia.Detalles[i].Valor);
                                cmd.ExecuteNonQueryConLog();
                            }
                        }

                        tx.CommitConLog();
                    }
                    catch (SqlException ex) when (ex.Number == SqlErrorViolacionForanea)
                    {
                        // El paciente no existe.
                        tx.RollbackConLog();
                        return null;
                    }
                    catch
                    {
                        tx.RollbackConLog();
                        throw;
                    }
                }
            }

            return Obtener(constancia.PacienteId, id);
        }

        public List<ConstanciaPaciente> ObtenerPorPaciente(int pacienteId)
        {
            return Leer(pacienteId, null);
        }

        public ConstanciaPaciente Obtener(int pacienteId, int constanciaId)
        {
            return Leer(pacienteId, constanciaId).FirstOrDefault();
        }

        private List<ConstanciaPaciente> Leer(int pacienteId, int? constanciaId)
        {
            string filtro = constanciaId == null ? string.Empty : " AND c.ConstanciaID = @ConstanciaID";

            const string sqlDetalles = @"
                SELECT d.ConstanciaID, d.Etiqueta, d.Valor
                FROM ConstanciaDetalles d
                JOIN ConstanciasPaciente c ON c.ConstanciaID = d.ConstanciaID
                WHERE c.PacienteID = @PacienteID{0}
                ORDER BY d.ConstanciaID, d.Orden;";

            var lista = new List<ConstanciaPaciente>();
            using (var conn = new SqlConnection(_connectionString))
            {
                conn.AbrirConLog();

                using (var cmd = new SqlCommand(Seleccion + filtro + " ORDER BY c.FechaEmision DESC, c.ConstanciaID DESC;", conn))
                {
                    cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                    cmd.Parameters.AddWithValue("@ConstanciaID", (object)constanciaId ?? DBNull.Value);
                    using (var reader = cmd.ExecuteReaderConLog())
                    {
                        while (reader.Read())
                        {
                            lista.Add(new ConstanciaPaciente
                            {
                                Id = reader.GetInt32(0),
                                Numero = reader.GetString(1),
                                PacienteId = reader.GetInt32(2),
                                NombrePaciente = reader.GetString(3),
                                Tipo = reader.GetString(4),
                                Titulo = reader.GetString(5),
                                Descripcion = reader.IsDBNull(6) ? null : reader.GetString(6),
                                FechaEmision = reader.GetDateTime(7),
                                ReferenciaId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                Detalles = new List<DetalleConstancia>()
                            });
                        }
                    }
                }

                var porId = lista.ToDictionary(c => c.Id);
                using (var cmd = new SqlCommand(string.Format(sqlDetalles, filtro), conn))
                {
                    cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                    cmd.Parameters.AddWithValue("@ConstanciaID", (object)constanciaId ?? DBNull.Value);
                    using (var reader = cmd.ExecuteReaderConLog())
                    {
                        while (reader.Read())
                        {
                            ConstanciaPaciente constancia;
                            if (porId.TryGetValue(reader.GetInt32(0), out constancia))
                            {
                                constancia.Detalles.Add(new DetalleConstancia
                                {
                                    Etiqueta = reader.GetString(1),
                                    Valor = reader.GetString(2)
                                });
                            }
                        }
                    }
                }
            }
            return lista;
        }
    }
}
