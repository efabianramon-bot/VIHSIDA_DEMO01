using System;

namespace Conavihsida.Core.Domain.Vistas
{
    /// <summary>
    /// Lo que Caja necesita saber de un paciente para cobrar: si existe y
    /// si su entrega va subsidiada. A proposito NO lleva nombre, codigo
    /// serologico ni ningun dato clinico (RN-01). La forma de la respuesta
    /// esta acordada con Caja: no cambiar los nombres de las propiedades.
    /// </summary>
    public class SubsidioVista
    {
        public bool Existe { get; set; }
        public bool TieneSubsidio { get; set; }

        /// <summary>0 o 100. RN-02: el ARV se subsidia al 100 %.</summary>
        public int Porcentaje { get; set; }

        /// <summary>Siempre null por ahora: el subsidio no tiene fecha de vencimiento.</summary>
        public DateTime? VigenteHasta { get; set; }
    }
}
