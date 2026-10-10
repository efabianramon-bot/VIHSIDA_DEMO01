using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient; // Si instalaste System.Data.SqlClient, cámbialo aquí
using Conavihsida.Core.Models;
using BCrypt.Net;

namespace Conavihsida.Core.Data
{
    using System.Data.SqlClient;
    public class UsuarioRepository : Conavihsida.Core.Domain.Repositorios.IUsuarioRepository
    {
        private readonly string _connectionString;
        
        public UsuarioRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Usuario ObtenerPorUsername(string username)
        {
            Usuario usuario = null;

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_ObtenerUsuarioParaLogin", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Username", username);

                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    // Primer conjunto de datos: Datos generales del usuario
                    if (reader.Read())
                    {
                        usuario = new Usuario
                        {
                            UsuarioID = reader.GetInt32(reader.GetOrdinal("UsuarioID")),
                            Username = reader.GetString(reader.GetOrdinal("Username")),
                            Email = reader.GetString(reader.GetOrdinal("Email")),
                            Nombres = reader.GetString(reader.GetOrdinal("Nombres")),
                            Apellidos = reader.GetString(reader.GetOrdinal("Apellidos")),
                            PasswordHash = reader.GetString(reader.GetOrdinal("PasswordHash")),
                            PasswordSalt = reader.IsDBNull(reader.GetOrdinal("PasswordSalt")) ? null : reader.GetString(reader.GetOrdinal("PasswordSalt")),
                            Activo = reader.GetBoolean(reader.GetOrdinal("Activo")),
                            Bloqueado = reader.GetBoolean(reader.GetOrdinal("Bloqueado")),
                            IntentosFallidos = reader.GetInt32(reader.GetOrdinal("IntentosFallidos"))
                        };
                    }

                    // Segundo conjunto de datos: Roles asignados
                    if (usuario != null && reader.NextResult())
                    {
                        while (reader.Read())
                        {
                            usuario.Roles.Add(reader.GetString(reader.GetOrdinal("NombreRol")));
                        }
                    }
                }
            }

            return usuario;
        }

        public void RegistrarIntentoLogin(int? usuarioId, string username, bool exitoso, string origen, string ip, string mensajeError)
        {
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("sp_RegistrarIntentoLogin", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@UsuarioID", (object)usuarioId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UsernameIngresado", username);
                cmd.Parameters.AddWithValue("@Exitoso", exitoso);
                cmd.Parameters.AddWithValue("@OrigenAcceso", origen);
                cmd.Parameters.AddWithValue("@DireccionIP", (object)ip ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@MensajeError", (object)mensajeError ?? DBNull.Value);

                conn.AbrirConLog();
                cmd.ExecuteNonQueryConLog();
            }
        }

        public ResultadoLogin Autenticar(string username, string password, string origenAcceso, string direccionIp = "127.0.0.1")
        {
            var usuario = ObtenerPorUsername(username);

            if (usuario == null)
            {
                RegistrarIntentoLogin(null, username, false, origenAcceso, direccionIp, "Usuario no existe");
                return new ResultadoLogin { Exitoso = false, Mensaje = "Credenciales incorrectas." };
            }

            if (!usuario.Activo)
            {
                RegistrarIntentoLogin(usuario.UsuarioID, username, false, origenAcceso, direccionIp, "Usuario inactivo");
                return new ResultadoLogin { Exitoso = false, Mensaje = "La cuenta se encuentra inactiva." };
            }

            if (usuario.Bloqueado)
            {
                RegistrarIntentoLogin(usuario.UsuarioID, username, false, origenAcceso, direccionIp, "Cuenta bloqueada por intentos");
                return new ResultadoLogin { Exitoso = false, Mensaje = "Cuenta bloqueada temporalmente." };
            }

            bool passwordValido = false;
            try
            {
                passwordValido = BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
            }
            catch
            {
                passwordValido = false;
            }

            if (!passwordValido)
            {
                RegistrarIntentoLogin(usuario.UsuarioID, username, false, origenAcceso, direccionIp, "Contraseña incorrecta");
                return new ResultadoLogin { Exitoso = false, Mensaje = "Credenciales incorrectas." };
            }

            RegistrarIntentoLogin(usuario.UsuarioID, username, true, origenAcceso, direccionIp, null);
            usuario.PasswordHash = null;
            usuario.PasswordSalt = null;

            return new ResultadoLogin
            {
                Exitoso = true,
                Mensaje = "Autenticación exitosa",
                Usuario = usuario
            };
        }

        public Conavihsida.Core.Domain.Entidades.UsuarioAutenticado ValidarCredenciales(
            string username, string password, string origenAcceso, string direccionIp, out string mensajeError)
        {
            var resultado = Autenticar(username, password, origenAcceso, direccionIp);
            if (!resultado.Exitoso)
            {
                mensajeError = resultado.Mensaje;
                return null;
            }

            mensajeError = null;
            return new Conavihsida.Core.Domain.Entidades.UsuarioAutenticado
            {
                Id = resultado.Usuario.UsuarioID,
                Username = resultado.Usuario.Username,
                Nombres = resultado.Usuario.Nombres,
                Apellidos = resultado.Usuario.Apellidos,
                Roles = resultado.Usuario.Roles
            };
        }

        public static string GenerarHashPassword(string passwordPlana)
        {
            return BCrypt.Net.BCrypt.HashPassword(passwordPlana, workFactor: 11);
        }
    }
}