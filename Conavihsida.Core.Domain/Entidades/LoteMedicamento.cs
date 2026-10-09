using System;

namespace Conavihsida.Core.Domain.Entidades
{
    public class LoteMedicamento
    {
        public int Id { get; set; }
        public int MedicamentoId { get; set; }
        public string NumeroLote { get; set; }
        public DateTime FechaCaducidad { get; set; }
        public int ExistenciaActual { get; set; }
        public int ExistenciaMinima { get; set; }
        public int? ExistenciaMaxima { get; set; }
    }
}
