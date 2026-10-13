using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class PacienteRepository : IPacienteRepository
    {
        // Numeros de error de SQL Server para violacion de UNIQUE.
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;

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

        public PacienteAutenticado Registrar(string nombreCompleto, string cedula, string correo, string password,
                                             DateTime? fechaNacimiento, string telefono, DateTime ahora)
        {
            // PasswordSalt es NOT NULL en la tabla, pero BCrypt guarda la
            // sal dentro del propio hash: queda vacia.
            const string sql = @"
                INSERT INTO Pacientes (Correo, PasswordHash, PasswordSalt, NombreCompleto, Cedula,
                                       FechaNacimiento, Telefono, Activo, FechaRegistro)
                OUTPUT INSERTED.PacienteID
                VALUES (@Correo, @PasswordHash, '', @NombreCompleto, @Cedula, @FechaNacimiento, @Telefono, 1, @Ahora);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.AddWithValue("@PasswordHash", UsuarioRepository.GenerarHashPassword(password));
                cmd.Parameters.AddWithValue("@NombreCompleto", nombreCompleto);
                cmd.Parameters.AddWithValue("@Cedula", cedula);
                cmd.Parameters.Add("@FechaNacimiento", SqlDbType.Date).Value = (object)fechaNacimiento ?? DBNull.Value;
                cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = (object)telefono ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@Ahora", ahora);

                conn.AbrirConLog();
                try
                {
                    int id = Convert.ToInt32(cmd.ExecuteScalarConLog());
                    return new PacienteAutenticado { PacienteId = id, NombreCompleto = nombreCompleto, Correo = correo };
                }
                catch (SqlException ex) when (ex.Number == SqlErrorViolacionUnique
                                            || ex.Number == SqlErrorViolacionUniqueConstraint)
                {
                    // Correo o cedula repetidos (los dos son unicos). Puede
                    // ser un paciente que un empleado registro en consulta
                    // y que ahora crea su cuenta.
                    return ActivarCuenta(conn, cedula, correo, password, telefono);
                }
            }
        }

        /// <summary>
        /// Da acceso a la web a un paciente que un empleado registro sin
        /// cuenta. Solo si coinciden la cedula Y el correo que se anoto en
        /// la consulta: con la cedula sola, cualquiera que la conociera
        /// podria quedarse con la cuenta. null si no aplica.
        /// </summary>
        private static PacienteAutenticado ActivarCuenta(SqlConnection conn, string cedula, string correo,
                                                         string password, string telefono)
        {
            const string sql = @"
                UPDATE Pacientes SET PasswordHash = @PasswordHash, IntentosFallidos = 0, BloqueadoHasta = NULL,
                                     Telefono = COALESCE(@Telefono, Telefono)
                OUTPUT INSERTED.PacienteID, INSERTED.NombreCompleto
                WHERE Cedula = @Cedula AND Correo = @Correo AND Activo = 1 AND PasswordHash NOT LIKE '$2%';";

            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@PasswordHash", UsuarioRepository.GenerarHashPassword(password));
                cmd.Parameters.AddWithValue("@Cedula", cedula);
                cmd.Parameters.AddWithValue("@Correo", correo);
                cmd.Parameters.Add("@Telefono", SqlDbType.VarChar, 20).Value = (object)telefono ?? DBNull.Value;
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    // Se conserva el nombre que registro el empleado, que
                    // lo tomo de la cedula del paciente.
                    return new PacienteAutenticado
                    {
                        PacienteId = reader.GetInt32(0),
                        NombreCompleto = reader.GetString(1),
                        Correo = correo
                    };
                }
            }
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
