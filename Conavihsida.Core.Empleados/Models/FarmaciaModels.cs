using System;
using System.Collections.Generic;
using System.Linq;

namespace Conavihsida.Core.Empleados.Models
{
    // ----- Lo que devuelve la API (api/farmacia) -----

    public class LoteApi
    {
        public int Id { get; set; }
        public int MedicamentoId { get; set; }
        public string NumeroLote { get; set; }
        public DateTime FechaCaducidad { get; set; }
        public int ExistenciaActual { get; set; }
        public int ExistenciaMinima { get; set; }
        public int? ExistenciaMaxima { get; set; }
        public string MedicamentoNombre { get; set; }
        public bool Vencido { get; set; }
        public bool BajoMinimo { get; set; }
        public int DiasParaCaducar { get; set; }

        /// <summary>Vigente, con existencia, y caduca dentro del plazo de aviso.</summary>
        public bool PorVencer
        {
            get { return !Vencido && ExistenciaActual > 0 && DiasParaCaducar <= FarmaciaInicioViewModel.DiasAviso; }
        }

        /// <summary>Se puede despachar de este lote.</summary>
        public bool Despachable
        {
            get { return !Vencido && ExistenciaActual > 0; }
        }
    }

    public class MovimientoApi
    {
        public int Id { get; set; }
        public int LoteId { get; set; }
        public string TipoMovimiento { get; set; }
        public int? RecetaId { get; set; }
        public int Cantidad { get; set; }
        public DateTime FechaMovimiento { get; set; }
        public string NumeroLote { get; set; }
        public string UsuarioNombre { get; set; }
    }

    /// <summary>Comprobante de despacho imprimible de una receta ya dispensada.</summary>
    public class ComprobanteDespachoViewModel
    {
        public RecetaApi Receta { get; set; }
        public string MedicamentoNombre { get; set; }
        public List<MovimientoApi> Salidas { get; set; }

        public int TotalEntregado
        {
            get { return Salidas.Sum(m => m.Cantidad); }
        }

        public DateTime FechaDespacho
        {
            get { return Salidas.Max(m => m.FechaMovimiento); }
        }

        public string Farmaceutico
        {
            get { return Salidas[0].UsuarioNombre; }
        }
    }

    // ----- Pantallas del Portal Farmacia -----

    public class FarmaciaInicioViewModel
    {
        /// <summary>Con cuantos dias de anticipacion se avisa de un lote por vencer.</summary>
        public const int DiasAviso = 60;

        public List<LoteApi> Lotes { get; set; }
        public List<RecetaApi> RecetasPendientes { get; set; }
        public List<MedicamentoApi> Medicamentos { get; set; }
        public string Error { get; set; }
        public string Exito { get; set; }

        // Un lote vencido solo es un problema si todavia tiene existencia
        // que retirar; uno agotado y vigente si lo es: hay que reponerlo.
        public int VencidosConExistencia
        {
            get { return Lotes.Count(l => l.Vencido && l.ExistenciaActual > 0); }
        }

        public int PorVencer
        {
            get { return Lotes.Count(l => l.PorVencer); }
        }

        public int BajoMinimo
        {
            get { return Lotes.Count(l => !l.Vencido && l.BajoMinimo); }
        }

        public string NombreMedicamento(int medicamentoId)
        {
            var medicamento = Medicamentos == null ? null : Medicamentos.Find(m => m.Id == medicamentoId);
            return medicamento == null ? "Medicamento n.º " + medicamentoId : medicamento.NombreCompleto;
        }
    }

    public class RegistrarLoteForm
    {
        public int? MedicamentoId { get; set; }
        public string NumeroLote { get; set; }
        public DateTime? FechaCaducidad { get; set; }
        public int? Cantidad { get; set; }
        public int? ExistenciaMinima { get; set; }
        public int? ExistenciaMaxima { get; set; }
    }

    public class RegistrarLoteViewModel
    {
        public RegistrarLoteForm Lote { get; set; }
        public List<MedicamentoApi> Medicamentos { get; set; }
        public string Error { get; set; }
    }

    public class RecetaViewModel
    {
        public RecetaApi Receta { get; set; }
        public string MedicamentoNombre { get; set; }

        /// <summary>Lotes del medicamento de los que se puede despachar, el que caduca antes primero.</summary>
        public List<LoteApi> LotesDisponibles { get; set; }
        public string Error { get; set; }
        public string Exito { get; set; }

        public int ExistenciaDisponible
        {
            get { return LotesDisponibles.Sum(l => l.ExistenciaActual); }
        }

        public bool EstaActiva
        {
            get { return Receta.Estado == "Activa"; }
        }

        public bool AlcanzaLaExistencia
        {
            get { return ExistenciaDisponible >= Receta.CantidadPrescrita; }
        }
    }
}
