namespace Conavihsida.Core.Data
{
    /// <summary>
    /// Trozos de SQL sobre Recetas que usan mas de un repositorio, para que
    /// la regla este escrita una sola vez.
    /// </summary>
    internal static class SqlRecetas
    {
        /// <summary>
        /// Columna FechaDescargo: cuando registro Caja la entrega de esa
        /// receta, o NULL si todavia no. Solo cuenta una transaccion
        /// Completada: una Pendiente o Anulada no es un descargo.
        /// La consulta debe leer de la tabla Recetas sin alias.
        /// </summary>
        public const string ColumnaFechaDescargo = @"
                       (SELECT MIN(t.Fecha) FROM CajaTransacciones t
                        WHERE t.NumeroReceta = Recetas.RecetaID AND t.Estado = 'Completada') AS FechaDescargo";
    }
}
