using System;

namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// RF-CORE-02: unica tabla que une el codigo serologico con la
    /// identidad civil. Solo el rol Medico deberia poder resolver esta union.
    /// </summary>
    public class ExpedienteClinico
    {
        public string CodigoSerologico { get; set; }
        public int PacienteId { get; set; }
        public int? MedicoResponsableId { get; set; }
        public DateTime FechaApertura { get; set; }
        public string Estado { get; set; }
    }
}
