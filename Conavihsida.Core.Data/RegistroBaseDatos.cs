using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Conavihsida.Core.Data
{
    public enum NivelRegistro
    {
        Info,
        Advertencia,
        Error
    }

    /// <summary>
    /// Log de base de datos: errores de SQL, fallos de conexion, consultas
    /// lentas y cada COMMIT / ROLLBACK. Esta capa no conoce NLog: quien
    /// arranca la aplicacion asigna Escritor (la API lo hace en
    /// LogConfig). Si nadie lo asigna, no se escribe nada.
    ///
    /// NUNCA se registran los valores de los parametros ni el resultado de
    /// las consultas: podrian traer datos clinicos o personales (RN-01).
    /// </summary>
    public static class RegistroBaseDatos
    {
        /// <summary>RNF-04: una operacion habitual debe tardar menos de 500 ms.</summary>
        public const int UmbralLentoMs = 500;

        public static Action<NivelRegistro, string>? Escritor { get; set; }

        internal static void Escribir(NivelRegistro nivel, string mensaje)
        {
            try
            {
                Escritor?.Invoke(nivel, mensaje);
            }
            catch
            {
                // Un fallo al escribir el log no debe tumbar la operacion.
            }
        }
    }

    /// <summary>
    /// Reemplazos de Open / Execute* / Commit / Rollback que hacen lo mismo
    /// que el original y ademas dejan constancia en el log de base de datos.
    /// El nombre de la operacion ("CitaRepository.Insertar") lo pone el
    /// compilador; no hay que pasarlo.
    /// </summary>
    internal static class SqlConLog
    {
        // Violacion de UNIQUE / PRIMARY KEY y de clave foranea: los
        // repositorios las esperan y las convierten en un rechazo de
        // negocio, asi que van como advertencia y no como error.
        private const int ViolacionUnique = 2601;
        private const int ViolacionUniqueConstraint = 2627;
        private const int ViolacionForanea = 547;

        private const int LargoMaximoMensaje = 300;

        public static void AbrirConLog(this DbConnection conexion,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            try
            {
                conexion.Open();
            }
            catch (Exception ex)
            {
                RegistroBaseDatos.Escribir(NivelRegistro.Error,
                    string.Format("CONEXION | {0} | {1}", Operacion(archivo, metodo), Limpiar(ex.Message)));
                throw;
            }
        }

        public static Microsoft.Data.SqlClient.SqlDataReader ExecuteReaderConLog(
            this Microsoft.Data.SqlClient.SqlCommand comando,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            return Medir(() => comando.ExecuteReader(), Operacion(archivo, metodo));
        }

        public static DbDataReader ExecuteReaderConLog(this DbCommand comando,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            return Medir(() => comando.ExecuteReader(), Operacion(archivo, metodo));
        }

        public static object? ExecuteScalarConLog(this DbCommand comando,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            return Medir(() => comando.ExecuteScalar(), Operacion(archivo, metodo));
        }

        public static int ExecuteNonQueryConLog(this DbCommand comando,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            return Medir(() => comando.ExecuteNonQuery(), Operacion(archivo, metodo));
        }

        public static void CommitConLog(this DbTransaction transaccion,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            transaccion.Commit();
            RegistroBaseDatos.Escribir(NivelRegistro.Info, "COMMIT | " + Operacion(archivo, metodo));
        }

        public static void RollbackConLog(this DbTransaction transaccion,
            [CallerMemberName] string metodo = "", [CallerFilePath] string archivo = "")
        {
            transaccion.Rollback();
            RegistroBaseDatos.Escribir(NivelRegistro.Advertencia, "ROLLBACK | " + Operacion(archivo, metodo));
        }

        private static T Medir<T>(Func<T> ejecutar, string operacion)
        {
            var cronometro = Stopwatch.StartNew();
            try
            {
                T resultado = ejecutar();
                cronometro.Stop();

                if (cronometro.ElapsedMilliseconds > RegistroBaseDatos.UmbralLentoMs)
                {
                    RegistroBaseDatos.Escribir(NivelRegistro.Advertencia,
                        string.Format("LENTA | {0} | {1} ms", operacion, cronometro.ElapsedMilliseconds));
                }
                return resultado;
            }
            catch (Exception ex)
            {
                RegistrarError(ex, operacion);
                throw;
            }
        }

        private static void RegistrarError(Exception ex, string operacion)
        {
            int? numero = null;
            int severidad = 0;

            var errorMicrosoft = ex as Microsoft.Data.SqlClient.SqlException;
            var errorSystem = ex as System.Data.SqlClient.SqlException;
            if (errorMicrosoft != null)
            {
                numero = errorMicrosoft.Number;
                severidad = errorMicrosoft.Class;
            }
            else if (errorSystem != null)
            {
                numero = errorSystem.Number;
                severidad = errorSystem.Class;
            }

            if (numero == null)
            {
                RegistroBaseDatos.Escribir(NivelRegistro.Error,
                    string.Format("ERROR | {0} | {1}: {2}", operacion, ex.GetType().Name, Limpiar(ex.Message)));
                return;
            }

            bool esRestriccion = numero == ViolacionUnique || numero == ViolacionUniqueConstraint
                || numero == ViolacionForanea;

            RegistroBaseDatos.Escribir(
                esRestriccion ? NivelRegistro.Advertencia : NivelRegistro.Error,
                string.Format("{0} | {1} | numero={2} severidad={3} | {4}",
                    esRestriccion ? "RESTRICCION" : "ERROR_SQL", operacion, numero, severidad,
                    Limpiar(ex.Message)));
        }

        private static string Operacion(string archivo, string metodo)
        {
            return Path.GetFileNameWithoutExtension(archivo) + "." + metodo;
        }

        // SQL Server incluye el valor repetido en el mensaje de una
        // violacion de UNIQUE ("The duplicate key value is (...)"): podria
        // ser una cedula o un correo, asi que se quita.
        private static string Limpiar(string mensaje)
        {
            string limpio = Regex.Replace(mensaje ?? string.Empty,
                @"The duplicate key value is \(.*?\)\.?", "[valor omitido]", RegexOptions.Singleline);
            limpio = Regex.Replace(limpio, @"\s+", " ").Trim();
            return limpio.Length > LargoMaximoMensaje ? limpio.Substring(0, LargoMaximoMensaje) + "..." : limpio;
        }
    }
}
