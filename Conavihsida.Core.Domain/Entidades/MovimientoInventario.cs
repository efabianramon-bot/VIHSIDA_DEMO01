using System;

namespace Conavihsida.Core.Domain.Entidades
{
    public class MovimientoInventario
    {
        public int Id { get; set; }
        public int LoteId { get; set; }

        /// <summary>Ver Catalogos.TiposMovimiento.</summary>
        public string TipoMovimiento { get; set; }

        /// <summary>Obligatorio cuando TipoMovimiento = Salida (RF-CORE-04).</summary>
        public int? RecetaId { get; set; }
        public int Cantidad { get; set; }
        public int UsuarioId { get; set; }
        public DateTime FechaMovimiento { get; set; }
    }
}
