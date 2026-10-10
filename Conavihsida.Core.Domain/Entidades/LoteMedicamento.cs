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

        /// <summary>Solo lectura: lo llena el repositorio con un JOIN a medicamentos.</summary>
        public string MedicamentoNombre { get; set; }

        /// <summary>Un lote vencido no se puede despachar.</summary>
        public bool Vencido
        {
            get { return FechaCaducidad.Date < DateTime.Today; }
        }

        /// <summary>Existencia en el minimo o por debajo: hay que reponer.</summary>
        public bool BajoMinimo
        {
            get { return ExistenciaActual <= ExistenciaMinima; }
        }
    }
}
