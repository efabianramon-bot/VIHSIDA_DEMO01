using Microsoft.Data.SqlClient;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Data
{
    public class AuditoriaRepository : IAuditoriaRepository
    {
        private readonly string _connectionString;

        public AuditoriaRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Registrar(RegistroAuditoria registro)
        {
            const string sql = @"
                INSERT INTO Auditoria
                    (UsuarioID, PacienteID, Terminal, Accion, EntidadAfectada, EntidadID, FechaHora, Detalle)
                VALUES
                    (@UsuarioID, @PacienteID, @Terminal, @Accion, @EntidadAfectada, @EntidadID, @FechaHora, @Detalle);";

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UsuarioID", (object)registro.UsuarioId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@PacienteID", (object)registro.PacienteId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Terminal", (object)registro.Terminal ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@Accion", registro.Accion);
                cmd.Parameters.AddWithValue("@EntidadAfectada", registro.EntidadAfectada);
                cmd.Parameters.AddWithValue("@EntidadID", (object)registro.EntidadId ?? System.DBNull.Value);
                cmd.Parameters.AddWithValue("@FechaHora", registro.FechaHora);
                cmd.Parameters.AddWithValue("@Detalle", (object)registro.Detalle ?? System.DBNull.Value);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}
