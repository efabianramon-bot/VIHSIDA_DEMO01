using System;
using System.Collections.Generic;

namespace Conavihsida.Core.Empleados.Models
{
    // ----- Lo que devuelve la API (api/clinica y api/farmacia/medicamentos) -----

    public class ExpedienteApi
    {
        public string CodigoSerologico { get; set; }
        public int PacienteId { get; set; }
        public int? MedicoResponsableId { get; set; }
        public DateTime FechaApertura { get; set; }
        public string Estado { get; set; }
    }

    public class PacienteApi
    {
        public int PacienteId { get; set; }
        public string NombreCompleto { get; set; }
        public string Cedula { get; set; }
        public DateTime? FechaNacimiento { get; set; }
        public string CodigoSerologico { get; set; }
        public string EstadoExpediente { get; set; }
    }

    public class ConsultaApi
    {
        public int Id { get; set; }
        public int UsuarioMedicoId { get; set; }
        public DateTime FechaConsulta { get; set; }
        public decimal? CargaViral { get; set; }
        public int? ConteoCd4 { get; set; }
        public string DiagnosticoDetalle { get; set; }
        public string NotasClinicas { get; set; }
        public List<RecetaApi> Recetas { get; set; }
    }

    public class RecetaApi
    {
        public int Id { get; set; }
        public int HistorialId { get; set; }
        public string CodigoSerologico { get; set; }
        public int MedicamentoId { get; set; }
        public string EsquemaTarv { get; set; }
        public int CantidadPrescrita { get; set; }
        public DateTime FechaEmision { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public string Estado { get; set; }
    }

    public class ResultadoLabApi
    {
        public int Id { get; set; }
        public string Prueba { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; }
        public string ValorResultado { get; set; }
        public DateTime? FechaResultado { get; set; }
        public string NotaPaciente { get; set; }

        public bool EstaListo
        {
            get { return Estado == "Listo"; }
        }
    }

    public class MedicamentoApi
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Presentacion { get; set; }
        public string UnidadMedida { get; set; }

        public string NombreCompleto
        {
            get { return string.IsNullOrEmpty(Presentacion) ? Nombre : Nombre + " " + Presentacion; }
        }
    }

    // ----- Pantallas del Portal Medico -----

    public class MedicoInicioViewModel
    {
        public List<ExpedienteApi> Expedientes { get; set; }
        public string Busqueda { get; set; }
        public string Error { get; set; }

        /// <summary>Paciente encontrado por cedula que aun no tiene expediente.</summary>
        public PacienteApi PacienteSinExpediente { get; set; }
    }

    public class ExpedienteViewModel
    {
        public ExpedienteApi Expediente { get; set; }
        public PacienteApi Paciente { get; set; }
        public List<ConsultaApi> Consultas { get; set; }
        public List<ResultadoLabApi> Resultados { get; set; }
        public List<MedicamentoApi> Medicamentos { get; set; }
        public NuevaConsultaForm NuevaConsulta { get; set; }
        public string Error { get; set; }
        public string Exito { get; set; }

        public bool AdmiteConsultas
        {
            get { return Expediente != null && Expediente.Estado == "Activo"; }
        }

        public string NombreMedicamento(int medicamentoId)
        {
            var medicamento = Medicamentos == null ? null : Medicamentos.Find(m => m.Id == medicamentoId);
            return medicamento == null ? "Medicamento n.º " + medicamentoId : medicamento.NombreCompleto;
        }
    }

    /// <summary>Formulario "Registrar consulta". Las recetas son opcionales.</summary>
    public class NuevaConsultaForm
    {
        public const int MaximoRecetas = 3;

        public string DiagnosticoDetalle { get; set; }
        public string NotasClinicas { get; set; }
        public decimal? CargaViral { get; set; }
        public int? ConteoCd4 { get; set; }
        public List<RecetaForm> Recetas { get; set; }

        public static NuevaConsultaForm Vacio()
        {
            var formulario = new NuevaConsultaForm { Recetas = new List<RecetaForm>() };
            for (int i = 0; i < MaximoRecetas; i++)
            {
                formulario.Recetas.Add(new RecetaForm());
            }
            return formulario;
        }
    }

    public class RecetaForm
    {
        /// <summary>0 o null = fila sin usar.</summary>
        public int? MedicamentoId { get; set; }
        public string EsquemaTarv { get; set; }
        public int? CantidadPrescrita { get; set; }
    }
}
