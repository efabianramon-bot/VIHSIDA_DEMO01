using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class CajaRepository : ICajaRepository
    {
        // Numeros de error de SQL Server para violacion de UNIQUE.
        private const int SqlErrorViolacionUnique = 2601;
        private const int SqlErrorViolacionUniqueConstraint = 2627;

        private readonly string _connectionString;

        public CajaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public bool GuardarTurno(TurnoCaja turno, DateTime ahora)
        {
            return Guardar("CajaTurnos", "NumeroTurno", turno.NumeroTurno, ahora, new Dictionary<string, object>
            {
                { "IdLocal", turno.IdLocal },
                { "UsuarioCajero", turno.UsuarioCajero },
                { "NombreCajero", turno.NombreCajero },
                { "Caja", turno.Caja },
                { "FechaApertura", turno.FechaApertura },
                { "FechaCierre", turno.FechaCierre },
                { "BalanceInicial", turno.BalanceInicial },
                { "SaldoEsperado", turno.SaldoEsperado },
                { "EfectivoDeclarado", turno.EfectivoDeclarado },
                { "Diferencia", turno.Diferencia },
                { "Estado", turno.Estado },
                { "Observacion", turno.Observacion }
            });
        }

        public bool GuardarMovimiento(MovimientoCaja movimiento, DateTime ahora)
        {
            return Guardar("CajaMovimientos", "NumeroMovimiento", movimiento.NumeroMovimiento, ahora,
                new Dictionary<string, object>
                {
                    { "IdLocal", movimiento.IdLocal },
                    { "TurnoCajaIdLocal", movimiento.TurnoCajaIdLocal },
                    { "Fecha", movimiento.Fecha },
                    { "Tipo", movimiento.Tipo },
                    { "Monto", movimiento.Monto },
                    { "Concepto", movimiento.Concepto },
                    { "Referencia", movimiento.Referencia },
                    { "UsuarioCajero", movimiento.UsuarioCajero }
                });
        }

        public bool? GuardarTransaccion(TransaccionCaja transaccion, DateTime ahora)
        {
            const string sqlReferencias = @"
                SELECT (SELECT PacienteID FROM Pacientes WHERE Cedula = @Cedula),
                       (SELECT COUNT(*)
                        FROM Recetas r
                        JOIN ExpedientesClinicos e ON e.CodigoSerologico = r.CodigoSerologico
                        JOIN Pacientes p ON p.PacienteID = e.PacienteID
                        WHERE r.RecetaID = @RecetaID AND p.Cedula = @Cedula);";

            int? pacienteId;
            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sqlReferencias, conn))
            {
                cmd.Parameters.AddWithValue("@Cedula", transaccion.CedulaPaciente);
                cmd.Parameters.AddWithValue("@RecetaID", (object)transaccion.NumeroReceta ?? DBNull.Value);
                conn.AbrirConLog();
                using (var reader = cmd.ExecuteReaderConLog())
                {
                    reader.Read();
                    pacienteId = reader.IsDBNull(0) ? (int?)null : reader.GetInt32(0);
                    if (transaccion.NumeroReceta != null && reader.GetInt32(1) == 0)
                    {
                        return null;
                    }
                }
            }

            return Guardar("CajaTransacciones", "NumeroTransaccion", transaccion.NumeroTransaccion, ahora,
                new Dictionary<string, object>
                {
                    { "IdLocal", transaccion.IdLocal },
                    { "Fecha", transaccion.Fecha },
                    { "CedulaPaciente", transaccion.CedulaPaciente },
                    { "PacienteID", pacienteId },
                    { "TipoServicio", transaccion.TipoServicio },
                    { "Monto", transaccion.Monto },
                    { "FormaPago", transaccion.FormaPago },
                    { "EsSubsidiado", transaccion.EsSubsidiado },
                    { "Estado", transaccion.Estado },
                    { "ComprobanteGenerado", transaccion.ComprobanteGenerado },
                    { "UsuarioCajero", transaccion.UsuarioCajero },
                    { "NumeroReceta", transaccion.NumeroReceta }
                });
        }

        public bool GuardarComprobante(ComprobanteCaja comprobante, DateTime ahora)
        {
            return Guardar("CajaComprobantes", "NumeroComprobante", comprobante.NumeroComprobante, ahora,
                new Dictionary<string, object>
                {
                    { "IdLocal", comprobante.IdLocal },
                    { "TransaccionIdLocal", comprobante.TransaccionIdLocal },
                    { "FechaGeneracion", comprobante.FechaGeneracion },
                    { "TipoDocumento", comprobante.TipoDocumento },
                    { "EsReimpresion", comprobante.EsReimpresion },
                    { "CantidadReimpresiones", comprobante.CantidadReimpresiones }
                });
        }

        public bool GuardarFondo(FondoConvenio fondo, DateTime ahora)
        {
            return Guardar("CajaFondos", "Codigo", fondo.Codigo, ahora, new Dictionary<string, object>
            {
                { "IdLocal", fondo.IdLocal },
                { "Nombre", fondo.Nombre },
                { "TipoFuente", fondo.TipoFuente },
                { "Organismo", fondo.Organismo },
                { "MontoAsignado", fondo.MontoAsignado },
                { "MontoUtilizado", fondo.MontoUtilizado },
                { "FechaInicio", fondo.FechaInicio },
                { "FechaFin", fondo.FechaFin },
                { "Activo", fondo.Activo }
            });
        }

        /// <summary>
        /// Actualiza la fila con esa clave o, si no existe, la inserta.
        /// tabla, columnaClave y los nombres de columna salen siempre de
        /// este archivo, nunca de lo que envia Caja; los valores van como
        /// parametros.
        /// </summary>
        private bool Guardar(string tabla, string columnaClave, string clave, DateTime ahora,
                             Dictionary<string, object> columnas)
        {
            var nombres = columnas.Keys.ToList();
            string sql = string.Format(@"
                UPDATE {0} SET {1}, FechaActualizado = @Ahora WHERE {2} = @Clave;
                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO {0} ({2}, {3}, FechaRecibido) VALUES (@Clave, {4}, @Ahora);
                    SELECT 1;
                END
                ELSE SELECT 0;",
                tabla,
                string.Join(", ", nombres.Select(n => n + " = @" + n)),
                columnaClave,
                string.Join(", ", nombres),
                string.Join(", ", nombres.Select(n => "@" + n)));

            // Si dos envios del mismo registro llegan a la vez, los dos ven
            // que no existe y uno choca con el UNIQUE: al reintentar, ese
            // ya encuentra la fila y la actualiza.
            for (int intento = 0; ; intento++)
            {
                try
                {
                    using (var conn = new SqlConnection(_connectionString))
                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.Parameters.AddWithValue("@Clave", clave);
                        cmd.Parameters.AddWithValue("@Ahora", ahora);
                        foreach (var columna in columnas)
                        {
                            cmd.Parameters.AddWithValue("@" + columna.Key, columna.Value ?? DBNull.Value);
                        }

                        conn.AbrirConLog();
                        return Convert.ToInt32(cmd.ExecuteScalarConLog()) == 1;
                    }
                }
                catch (SqlException ex) when (intento == 0 && (ex.Number == SqlErrorViolacionUnique
                                                            || ex.Number == SqlErrorViolacionUniqueConstraint))
                {
                }
            }
        }
    }
}
