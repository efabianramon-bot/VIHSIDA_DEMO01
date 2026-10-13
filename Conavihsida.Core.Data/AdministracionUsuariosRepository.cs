using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class AdministracionUsuariosRepository : IAdministracionUsuariosRepository
    {
        // Numeros de error de SQL Server: violacion de UNIQUE y de clave foranea.
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;
        private const int SqlErrorViolacionForanea = 547;

        private const string EspecialidadPorDefecto = "Medicina general";

        // El sistema maneja un rol por usuario; si la base trae mas de uno
        // (datos antiguos), se muestra el asignado mas recientemente.
        private const string Seleccion = @"
                SELECT u.UsuarioID, u.Username, u.Email, u.Nombres, u.Apellidos, u.FilialID,
                       f.Nombre AS FilialNombre, u.Activo, u.Bloqueado, u.FechaCreacion, u.FechaUltimoAcceso,
                       (SELECT TOP 1 r.NombreRol
                        FROM UsuarioRoles ur JOIN Roles r ON r.RolID = ur.RolID
                        WHERE ur.UsuarioID = u.UsuarioID
                        ORDER BY ur.FechaAsignacion DESC, r.RolID) AS Rol
                FROM Usuarios u
                LEFT JOIN Filiales f ON f.FilialID = u.FilialID";

        private readonly string _connectionString;

        public AdministracionUsuariosRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<UsuarioAdministrado> Listar()
        {
            var lista = new List<UsuarioAdministrado>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(Seleccion + " ORDER BY u.Apellidos, u.Nombres, u.UsuarioID;", conn))
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

        public UsuarioAdministrado Obtener(int usuarioId)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(Seleccion + " WHERE u.UsuarioID = @UsuarioID;", conn))
            {
                cmd.Parameters.AddWithValue("@UsuarioID", usuarioId);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    return reader.Read() ? Leer(reader) : null;
                }
            }
        }

        public List<Filial> ListarFiliales()
        {
            const string sql = "SELECT FilialID, Nombre, Activa FROM Filiales ORDER BY Nombre;";

            var lista = new List<Filial>();
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Filial
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1),
                            Activa = reader.GetBoolean(2)
                        });
                    }
                }
            }
            return lista;
        }

        public ResultadoGuardarUsuario Crear(UsuarioAdministrado usuario, string password, DateTime ahora)
        {
            const string sql = @"
                INSERT INTO Usuarios (Username, Email, Nombres, Apellidos, PasswordHash, FilialID, Activo, FechaCreacion)
                OUTPUT INSERTED.UsuarioID
                VALUES (@Username, @Email, @Nombres, @Apellidos, @PasswordHash, @FilialID, 1, @Ahora);";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.AbrirConLog();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var repetido = BuscarRepetido(conn, tx, usuario.Username, usuario.Email, null);
                        if (repetido != ResultadoGuardarUsuario.Guardado)
                        {
                            tx.RollbackConLog();
                            return repetido;
                        }

                        using (var cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Username", usuario.Username);
                            cmd.Parameters.AddWithValue("@Email", usuario.Email);
                            cmd.Parameters.AddWithValue("@Nombres", usuario.Nombres);
                            cmd.Parameters.AddWithValue("@Apellidos", usuario.Apellidos);
                            cmd.Parameters.AddWithValue("@PasswordHash", UsuarioRepository.GenerarHashPassword(password));
                            cmd.Parameters.Add("@FilialID", SqlDbType.Int).Value = (object)usuario.FilialId ?? DBNull.Value;
                            cmd.Parameters.AddWithValue("@Ahora", ahora);
                            usuario.Id = Convert.ToInt32(cmd.ExecuteScalarConLog());
                        }

                        if (!AsignarRol(conn, tx, usuario.Id, usuario.Rol, ahora))
                        {
                            tx.RollbackConLog();
                            return ResultadoGuardarUsuario.RolOFilialNoExiste;
                        }

                        tx.CommitConLog();
                        usuario.Activo = true;
                        usuario.FechaCreacion = ahora;
                        return ResultadoGuardarUsuario.Guardado;
                    }
                    catch (SqlException ex) when (EsRechazoConocido(ex))
                    {
                        tx.RollbackConLog();
                        return Traducir(ex);
                    }
                    catch
                    {
                        tx.RollbackConLog();
                        throw;
                    }
                }
            }
        }

        public ResultadoGuardarUsuario Actualizar(UsuarioAdministrado usuario, string password, DateTime ahora)
        {
            // Cambiar la contraseña tambien levanta el bloqueo por intentos
            // fallidos: es lo que se hace cuando alguien la olvido.
            const string sql = @"
                UPDATE Usuarios
                SET Nombres = @Nombres, Apellidos = @Apellidos, Email = @Email, FilialID = @FilialID,
                    FechaModificacion = @Ahora,
                    PasswordHash = COALESCE(@PasswordHash, PasswordHash),
                    Bloqueado = CASE WHEN @PasswordHash IS NULL THEN Bloqueado ELSE 0 END,
                    IntentosFallidos = CASE WHEN @PasswordHash IS NULL THEN IntentosFallidos ELSE 0 END
                WHERE UsuarioID = @UsuarioID;";

            using (var conn = new SqlConnection(_connectionString))
            {
                conn.AbrirConLog();
                using (var tx = conn.BeginTransaction())
                {
                    try
                    {
                        var repetido = BuscarRepetido(conn, tx, null, usuario.Email, usuario.Id);
                        if (repetido != ResultadoGuardarUsuario.Guardado)
                        {
                            tx.RollbackConLog();
                            return repetido;
                        }

                        using (var cmd = new SqlCommand(sql, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@Nombres", usuario.Nombres);
                            cmd.Parameters.AddWithValue("@Apellidos", usuario.Apellidos);
                            cmd.Parameters.AddWithValue("@Email", usuario.Email);
                            cmd.Parameters.Add("@FilialID", SqlDbType.Int).Value = (object)usuario.FilialId ?? DBNull.Value;
                            cmd.Parameters.AddWithValue("@Ahora", ahora);
                            cmd.Parameters.Add("@PasswordHash", SqlDbType.VarChar, 255).Value = password == null
                                ? (object)DBNull.Value : UsuarioRepository.GenerarHashPassword(password);
                            cmd.Parameters.AddWithValue("@UsuarioID", usuario.Id);

                            if (cmd.ExecuteNonQueryConLog() == 0)
                            {
                                tx.RollbackConLog();
                                return ResultadoGuardarUsuario.NoExiste;
                            }
                        }

                        if (!AsignarRol(conn, tx, usuario.Id, usuario.Rol, ahora))
                        {
                            tx.RollbackConLog();
                            return ResultadoGuardarUsuario.RolOFilialNoExiste;
                        }

                        tx.CommitConLog();
                        return ResultadoGuardarUsuario.Guardado;
                    }
                    catch (SqlException ex) when (EsRechazoConocido(ex))
                    {
                        tx.RollbackConLog();
                        return Traducir(ex);
                    }
                    catch
                    {
                        tx.RollbackConLog();
                        throw;
                    }
                }
            }
        }

        public bool CambiarEstado(int usuarioId, bool activo, DateTime ahora)
        {
            return Ejecutar("UPDATE Usuarios SET Activo = @Valor, FechaModificacion = @Ahora WHERE UsuarioID = @UsuarioID;",
                usuarioId, activo, ahora);
        }

        public bool Desbloquear(int usuarioId, DateTime ahora)
        {
            return Ejecutar(@"UPDATE Usuarios SET Bloqueado = @Valor, IntentosFallidos = 0, FechaModificacion = @Ahora
                              WHERE UsuarioID = @UsuarioID;", usuarioId, false, ahora);
        }

        private bool Ejecutar(string sql, int usuarioId, bool valor, DateTime ahora)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Valor", valor);
                cmd.Parameters.AddWithValue("@Ahora", ahora);
                cmd.Parameters.AddWithValue("@UsuarioID", usuarioId);
                conn.AbrirConLog();
                return cmd.ExecuteNonQueryConLog() > 0;
            }
        }

        /// <summary>Guardado si ni el username ni el correo estan en uso por otro usuario.</summary>
        private static ResultadoGuardarUsuario BuscarRepetido(SqlConnection conn, SqlTransaction tx,
                                                              string username, string email, int? exceptoUsuarioId)
        {
            const string sql = @"
                SELECT (SELECT COUNT(*) FROM Usuarios WHERE Username = @Username),
                       (SELECT COUNT(*) FROM Usuarios WHERE Email = @Email
                          AND (@Excepto IS NULL OR UsuarioID <> @Excepto));";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.Add("@Username", SqlDbType.VarChar, 50).Value = (object)username ?? DBNull.Value;
                cmd.Parameters.AddWithValue("@Email", email);
                cmd.Parameters.Add("@Excepto", SqlDbType.Int).Value = (object)exceptoUsuarioId ?? DBNull.Value;
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    reader.Read();
                    if (reader.GetInt32(0) > 0)
                    {
                        return ResultadoGuardarUsuario.UsernameRepetido;
                    }
                    return reader.GetInt32(1) > 0
                        ? ResultadoGuardarUsuario.EmailRepetido
                        : ResultadoGuardarUsuario.Guardado;
                }
            }
        }

        /// <summary>
        /// Deja al usuario con ese unico rol. Un Medico necesita ademas su
        /// ficha en Medicos (la usan las citas y el medico responsable): se
        /// crea o se reactiva, y se desactiva si deja de ser Medico.
        /// false si el rol no existe.
        /// </summary>
        private static bool AsignarRol(SqlConnection conn, SqlTransaction tx, int usuarioId, string rol, DateTime ahora)
        {
            const string sql = @"
                DELETE FROM UsuarioRoles WHERE UsuarioID = @UsuarioID;

                INSERT INTO UsuarioRoles (UsuarioID, RolID, FechaAsignacion)
                SELECT @UsuarioID, RolID, @Ahora FROM Roles WHERE NombreRol = @Rol;

                IF @@ROWCOUNT = 0
                BEGIN
                    SELECT 0;
                    RETURN;
                END

                IF @Rol = @RolMedico
                BEGIN
                    IF EXISTS (SELECT 1 FROM Medicos WHERE UsuarioID = @UsuarioID)
                        UPDATE Medicos SET Activo = 1 WHERE UsuarioID = @UsuarioID;
                    ELSE
                        INSERT INTO Medicos (UsuarioID, Especialidad) VALUES (@UsuarioID, @Especialidad);
                END
                ELSE
                    UPDATE Medicos SET Activo = 0 WHERE UsuarioID = @UsuarioID;

                SELECT 1;";

            using (var cmd = new SqlCommand(sql, conn, tx))
            {
                cmd.Parameters.AddWithValue("@UsuarioID", usuarioId);
                cmd.Parameters.AddWithValue("@Ahora", ahora);
                cmd.Parameters.AddWithValue("@Rol", rol);
                cmd.Parameters.AddWithValue("@RolMedico", NombresRol.Medico);
                cmd.Parameters.AddWithValue("@Especialidad", EspecialidadPorDefecto);
                return Convert.ToInt32(cmd.ExecuteScalarConLog()) == 1;
            }
        }

        private static bool EsRechazoConocido(SqlException ex)
        {
            return ex.Number == SqlErrorViolacionUnique || ex.Number == SqlErrorViolacionUniqueConstraint
                || ex.Number == SqlErrorViolacionForanea;
        }

        // Una violacion de UNIQUE aqui solo puede ser por una carrera entre
        // dos altas simultaneas: BuscarRepetido ya reviso el caso normal.
        private static ResultadoGuardarUsuario Traducir(SqlException ex)
        {
            return ex.Number == SqlErrorViolacionForanea
                ? ResultadoGuardarUsuario.RolOFilialNoExiste
                : ResultadoGuardarUsuario.UsernameRepetido;
        }

        private static UsuarioAdministrado Leer(SqlDataReader reader)
        {
            int ordFilial = reader.GetOrdinal("FilialID");
            int ordFilialNombre = reader.GetOrdinal("FilialNombre");
            int ordAcceso = reader.GetOrdinal("FechaUltimoAcceso");
            int ordRol = reader.GetOrdinal("Rol");
            return new UsuarioAdministrado
            {
                Id = reader.GetInt32(reader.GetOrdinal("UsuarioID")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
                Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
                FilialId = reader.IsDBNull(ordFilial) ? (int?)null : reader.GetInt32(ordFilial),
                FilialNombre = reader.IsDBNull(ordFilialNombre) ? null : reader.GetString(ordFilialNombre),
                Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                Bloqueado = reader.GetBoolean(reader.GetOrdinal("Bloqueado")),
                FechaCreacion = reader.GetDateTime(reader.GetOrdinal("FechaCreacion")),
                FechaUltimoAcceso = reader.IsDBNull(ordAcceso) ? (DateTime?)null : reader.GetDateTime(ordAcceso),
                Rol = reader.IsDBNull(ordRol) ? null : reader.GetString(ordRol)
            };
        }
    }
}
