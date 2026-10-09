using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class CitaRepository : ICitaRepository
    {
        // Numero de error de SQL Server para violacion de indice/restriccion
        // UNIQUE (usado por UX_Citas_Medico_Fecha_Hora_Activa).
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;

        private readonly string _connectionString;

        public CitaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<Medico> ObtenerMedicos()
        {
            const string sql = @"
                SELECT m.MedicoID, m.UsuarioID, m.Especialidad, m.Activo,
                       (u.Nombres + ' ' + u.Apellidos) AS NombreCompleto
                FROM Medicos m
                INNER JOIN Usuarios u ON u.UsuarioID = m.UsuarioID
                WHERE m.Activo = 1
                ORDER BY u.Nombres, u.Apellidos;";

            var lista = new List<Medico>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(LeerMedico(reader));
                    }
                }
            }
            return lista;
        }

        public Medico ObtenerMedico(int medicoId)
        {
            const string sql = @"
                SELECT m.MedicoID, m.UsuarioID, m.Especialidad, m.Activo,
                       (u.Nombres + ' ' + u.Apellidos) AS NombreCompleto
                FROM Medicos m
                INNER JOIN Usuarios u ON u.UsuarioID = m.UsuarioID
                WHERE m.MedicoID = @MedicoID;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MedicoID", medicoId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return LeerMedico(reader);
                    }
                }
            }
            return null;
        }

        public List<Cita> ObtenerCitasPaciente(int pacienteId)
        {
            const string sql = @"
                SELECT c.CitaID, c.PacienteID, c.MedicoID, c.Fecha, c.Hora, c.Motivo,
                       c.Estado, c.FechaCreacion,
                       (u.Nombres + ' ' + u.Apellidos) AS MedicoNombre, m.Especialidad
                FROM Citas c
                INNER JOIN Medicos m ON m.MedicoID = c.MedicoID
                INNER JOIN Usuarios u ON u.UsuarioID = m.UsuarioID
                WHERE c.PacienteID = @PacienteID
                ORDER BY c.Fecha DESC, c.Hora DESC;";

            var lista = new List<Cita>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Cita
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("CitaID")),
                            PacienteId = reader.GetInt32(reader.GetOrdinal("PacienteID")),
                            MedicoId = reader.GetInt32(reader.GetOrdinal("MedicoID")),
                            Fecha = reader.GetDateTime(reader.GetOrdinal("Fecha")),
                            Hora = reader.GetString(reader.GetOrdinal("Hora")),
                            Motivo = reader.GetString(reader.GetOrdinal("Motivo")),
                            Estado = reader.GetString(reader.GetOrdinal("Estado")),
                            FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                            MedicoNombre = reader.GetString(reader.GetOrdinal("MedicoNombre")),
                            Especialidad = reader.GetString(reader.GetOrdinal("Especialidad"))
                        });
                    }
                }
            }
            return lista;
        }

        public List<string> ObtenerHorasOcupadas(int medicoId, DateTime fecha)
        {
            const string sql = @"
                SELECT Hora FROM Citas
                WHERE MedicoID = @MedicoID AND Fecha = @Fecha AND Estado <> @Cancelada;";

            var horas = new List<string>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MedicoID", medicoId);
                cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = fecha.Date;
                cmd.Parameters.AddWithValue("@Cancelada", EstadosCita.Cancelada);
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        horas.Add(reader.GetString(0));
                    }
                }
            }
            return horas;
        }

        public int? Insertar(Cita cita)
        {
            const string sql = @"
                INSERT INTO Citas (PacienteID, MedicoID, Fecha, Hora, Motivo, Estado, FechaCreacion)
                OUTPUT INSERTED.CitaID
                VALUES (@PacienteID, @MedicoID, @Fecha, @Hora, @Motivo, @Estado, @FechaCreacion);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PacienteID", cita.PacienteId);
                cmd.Parameters.AddWithValue("@MedicoID", cita.MedicoId);
                cmd.Parameters.Add("@Fecha", SqlDbType.Date).Value = cita.Fecha.Date;
                cmd.Parameters.AddWithValue("@Hora", cita.Hora);
                cmd.Parameters.AddWithValue("@Motivo", cita.Motivo);
                cmd.Parameters.AddWithValue("@Estado", cita.Estado);
                cmd.Parameters.AddWithValue("@FechaCreacion", cita.FechaCreacion);

                conn.Open();
                try
                {
                    object resultado = cmd.ExecuteScalar();
                    return Convert.ToInt32(resultado);
                }
                catch (SqlException ex) when (ex.Number == SqlErrorViolacionUnique
                                            || ex.Number == SqlErrorViolacionUniqueConstraint)
                {
                    // Alguien mas tomo ese horario primero (UX_Citas_Medico_Fecha_Hora_Activa).
                    return null;
                }
            }
        }

        public bool Cancelar(int citaId, int pacienteId)
        {
            const string sql = @"
                UPDATE Citas SET Estado = @Cancelada
                WHERE CitaID = @CitaID AND PacienteID = @PacienteID AND Estado <> @Cancelada;";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Cancelada", EstadosCita.Cancelada);
                cmd.Parameters.AddWithValue("@CitaID", citaId);
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                conn.Open();
                int filas = cmd.ExecuteNonQuery();
                return filas > 0;
            }
        }

        private static Medico LeerMedico(SqlDataReader reader)
        {
            return new Medico
            {
                Id = reader.GetInt32(reader.GetOrdinal("MedicoID")),
                UsuarioId = reader.GetInt32(reader.GetOrdinal("UsuarioID")),
                Especialidad = reader.GetString(reader.GetOrdinal("Especialidad")),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                NombreCompleto = reader.GetString(reader.GetOrdinal("NombreCompleto"))
            };
        }
    }
}
