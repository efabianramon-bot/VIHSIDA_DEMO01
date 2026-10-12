using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class PacienteRepository : IPacienteRepository
    {
        private readonly string _connectionString;

        public PacienteRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public ResultadoLoginPaciente ValidarCredenciales(string correo, string password, DateTime ahora,
                                                          int maximoIntentos, int minutosBloqueo,
                                                          out PacienteAutenticado paciente)
        {
            paciente = null;

            const string sqlBuscar = @"
                SELECT PacienteID, Correo, NombreCompleto, PasswordHash, Activo, IntentosFallidos, BloqueadoHasta
                FROM Pacientes WHERE Correo = @Correo;";

            int id;
            string correoGuardado, nombre, hash;
            bool activo;
            int intentos;
            DateTime? bloqueadoHasta;

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.AbrirConLog();

                using (var cmd = new SqlCommand(sqlBuscar, conn))
                {
                    cmd.Parameters.Add("@Correo", SqlDbType.VarChar, 150).Value = correo;
                    using (var reader = cmd.ExecuteReaderConLog())
                    {
                        if (!reader.Read())
                        {
                            return ResultadoLoginPaciente.CredencialesIncorrectas;
                        }

                        id = reader.GetInt32(0);
                        correoGuardado = reader.GetString(1);
                        nombre = reader.GetString(2);
                        hash = reader.GetString(3);
                        activo = reader.GetBoolean(4);
                        intentos = reader.GetInt32(5);
                        bloqueadoHasta = reader.IsDBNull(6) ? (DateTime?)null : reader.GetDateTime(6);
                    }
                }

                if (!activo)
                {
                    return ResultadoLoginPaciente.CredencialesIncorrectas;
                }

                // Mientras dura el bloqueo ni siquiera se compara la
                // contraseña: de eso se trata.
                if (bloqueadoHasta != null && bloqueadoHasta > ahora)
                {
                    return ResultadoLoginPaciente.BloqueadoTemporalmente;
                }

                if (!PasswordCoincide(password, hash))
                {
                    // Un bloqueo ya vencido reinicia la cuenta de intentos.
                    int nuevos = (bloqueadoHasta != null ? 0 : intentos) + 1;
                    bool bloquear = nuevos >= maximoIntentos;
                    Actualizar(conn, id, bloquear ? 0 : nuevos,
                        bloquear ? ahora.AddMinutes(minutosBloqueo) : (DateTime?)null);
                    return bloquear
                        ? ResultadoLoginPaciente.BloqueadoTemporalmente
                        : ResultadoLoginPaciente.CredencialesIncorrectas;
                }

                if (intentos != 0 || bloqueadoHasta != null)
                {
                    Actualizar(conn, id, 0, null);
                }
            }

            paciente = new PacienteAutenticado { PacienteId = id, NombreCompleto = nombre, Correo = correoGuardado };
            return ResultadoLoginPaciente.Correcto;
        }

        // Un hash que no es de BCrypt (por ejemplo el "SIN-ACCESO" de los
        // pacientes de prueba) hace que Verify lance una excepcion.
        private static bool PasswordCoincide(string password, string hash)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch
            {
                return false;
            }
        }

        private static void Actualizar(SqlConnection conn, int pacienteId, int intentos, DateTime? bloqueadoHasta)
        {
            const string sql = @"
                UPDATE Pacientes SET IntentosFallidos = @Intentos, BloqueadoHasta = @BloqueadoHasta
                WHERE PacienteID = @PacienteID;";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Intentos", intentos);
                cmd.Parameters.Add("@BloqueadoHasta", SqlDbType.DateTime).Value = (object)bloqueadoHasta ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@PacienteID", pacienteId);
                cmd.ExecuteNonQueryConLog();
            }
        }
    }
}
