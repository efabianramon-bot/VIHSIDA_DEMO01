using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>RN-01: datos clinicos. Solo accesibles con rol Medico.</summary>
    public class HistorialClinico
    {
        public int Id { get; set; }
        public string CodigoSerologico { get; set; }
        public int UsuarioMedicoId { get; set; }
        public DateTime FechaConsulta { get; set; }
        public decimal? CargaViral { get; set; }
        public int? ConteoCd4 { get; set; }
        public string DiagnosticoDetalle { get; set; }
        public string NotasClinicas { get; set; }
    }
}
