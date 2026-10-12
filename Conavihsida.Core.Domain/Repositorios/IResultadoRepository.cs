using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Vistas;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IResultadoRepository
    {
        /// <summary>
        /// La implementacion debe leer de la vista vw_resultados_paciente,
        /// NUNCA de la tabla resultados_lab (RN-01).
        /// </summary>
        List<ResultadoPacienteVista> ObtenerResultadosPaciente(int pacienteId);

        // ----- Lado clinico: solo para el rol Medico (RN-01) -----

        List<ResultadoLab> ObtenerPorExpediente(string codigoSerologico);

        /// <summary>null si no existe.</summary>
        ResultadoLab ObtenerPorId(int resultadoId);

        /// <summary>Devuelve el id generado.</summary>
        int Insertar(ResultadoLab resultado);

        /// <summary>
        /// Pasa el resultado a Listo con su valor. Si notaPaciente es null
        /// se conserva la nota que ya tenia. Devuelve false si el resultado
        /// no existe o ya estaba Listo.
        /// </summary>
        bool Publicar(int resultadoId, string valorResultado, string notaPaciente, DateTime fechaResultado);
    }
}
