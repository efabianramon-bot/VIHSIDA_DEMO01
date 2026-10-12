using System;
using System.Collections.Generic;
using System.Globalization;

namespace Conavihsida.Core.Empleados.Models
{
    // ----- Lo que devuelve GET api/dashboard (solo totales, RN-01) -----

    public class DashboardApi
    {
        public PacientesActivosApi Pacientes { get; set; }
        public AdherenciaApi Adherencia { get; set; }
        public InventarioCriticoApi Inventario { get; set; }
        public SubsidiosApi Subsidios { get; set; }
    }

    public class PacientesActivosApi
    {
        public int Dias { get; set; }
        public int Activos { get; set; }
        public int Inactivos { get; set; }
        public int Transferidos { get; set; }
        public int Total { get; set; }
        public int NuevosEnPeriodo { get; set; }
    }

    public class AdherenciaApi
    {
        public int Dias { get; set; }
        public int RecetasEmitidas { get; set; }
        public int RecetasDispensadas { get; set; }
        public int RecetasPendientes { get; set; }
        public int RecetasVencidas { get; set; }
        public int RecetasCanceladas { get; set; }
        public decimal? PorcentajeAdherencia { get; set; }
        public int PacientesConReceta { get; set; }
        public int PacientesConRetiro { get; set; }
    }

    public class InventarioCriticoApi
    {
        public int DiasAviso { get; set; }
        public int LotesVencidosConExistencia { get; set; }
        public int LotesPorVencer { get; set; }
        public int LotesBajoMinimo { get; set; }
        public List<LoteApi> Lotes { get; set; }
    }

    public class SubsidiosApi
    {
        public int Dias { get; set; }
        public int FacturasSubsidiadas { get; set; }
        public int FacturasDescargadas { get; set; }
        public int FacturasPendientes { get; set; }
        public decimal? PorcentajeEjecucion { get; set; }
        public int EntregasArv { get; set; }
        public int UnidadesArvEntregadas { get; set; }

        // Lo que sincroniza Caja.
        public int EntregasArvConDescargo { get; set; }
        public int EntregasSubsidiadasCaja { get; set; }
        public int CobrosCaja { get; set; }
        public decimal MontoCobradoCaja { get; set; }
        public int TransaccionesAnuladasCaja { get; set; }
        public List<FondoApi> Fondos { get; set; }
        public decimal MontoAsignadoFondos { get; set; }
        public decimal MontoUtilizadoFondos { get; set; }
        public decimal? PorcentajeEjecucionFondos { get; set; }

        public bool HayFondos
        {
            get { return Fondos != null && Fondos.Count > 0; }
        }
    }

    public class FondoApi
    {
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string TipoFuente { get; set; }
        public string Organismo { get; set; }
        public decimal MontoAsignado { get; set; }
        public decimal MontoUtilizado { get; set; }
        public decimal? PorcentajeUtilizado { get; set; }
        public DateTime? FechaFin { get; set; }

        public string Fuente
        {
            get { return TipoFuente == "DonacionInternacional" ? "Donación internacional" : "Presupuesto estatal"; }
        }
    }

    // ----- Pantalla del Dashboard -----

    public class DashboardViewModel
    {
        public static readonly int[] Periodos = { 30, 90, 180, 365 };

        public DashboardApi Datos { get; set; }
        public int Dias { get; set; }
        public string Error { get; set; }

        /// <summary>Parte de un total como porcentaje (0 a 100); 0 si el total es 0.</summary>
        public static decimal Porcentaje(int parte, int total)
        {
            return total <= 0 ? 0 : Math.Round(parte * 100m / total, 1);
        }

        /// <summary>
        /// Numero para un atributo de estilo o de SVG: siempre con punto
        /// decimal, sea cual sea la cultura del servidor.
        /// </summary>
        public static string Css(decimal valor)
        {
            return valor.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>Un grafico de anillo: que porcentaje mostrar y como rotularlo.</summary>
    public class AnilloViewModel
    {
        // Radio y circunferencia del anillo dentro de un viewBox de 120x120.
        public const decimal Radio = 52m;
        public const decimal Circunferencia = 326.73m;

        /// <summary>null = no hay datos para calcularlo.</summary>
        public decimal? Porcentaje { get; set; }
        public string Rotulo { get; set; }

        public string Trazo
        {
            get
            {
                decimal lleno = Circunferencia * Math.Min(Math.Max(Porcentaje ?? 0, 0), 100) / 100m;
                return DashboardViewModel.Css(lleno) + " " + DashboardViewModel.Css(Circunferencia);
            }
        }
    }

    // ----- Bitacora de auditoria -----

    public class RegistroAuditoriaApi
    {
        public int Id { get; set; }
        public int? UsuarioId { get; set; }
        public int? PacienteId { get; set; }
        public string Terminal { get; set; }
        public string Accion { get; set; }
        public string EntidadAfectada { get; set; }
        public int? EntidadId { get; set; }
        public DateTime FechaHora { get; set; }
        public string Detalle { get; set; }
    }

    public class BitacoraViewModel
    {
        // Acciones que registra la API; alimentan la lista del filtro.
        public static readonly string[] AccionesConocidas =
        {
            "AbrirExpediente", "AccesoDenegado", "ActualizarExpediente", "AgendarCita", "CancelarCita",
            "ConsultarExpediente", "ConsultarHistorial", "ConsultarReceta", "ConsultarResultados",
            "ConsultarResultadosClinicos", "DespacharReceta", "DespachoSinDescargo", "EmitirReceta",
            "IdentificarPaciente", "IniciarSesion", "IniciarSesionPaciente", "ListarExpedientes",
            "ListarRecetas", "LoginFallido", "LoginPacienteFallido", "PagarFactura", "PublicarResultado",
            "RegistrarConsulta", "RegistrarLote", "SincronizarCaja", "SolicitarPrueba", "ValidarSubsidio",
            "VencerRecetas"
        };

        public static readonly int[] Limites = { 50, 100, 200, 500 };

        public List<RegistroAuditoriaApi> Registros { get; set; }
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public int? UsuarioId { get; set; }
        public string Accion { get; set; }
        public int Maximo { get; set; }
        public string Error { get; set; }
    }
}
