using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IHistorialRepository
    {
        /// <summary>Consultas del expediente, la mas reciente primero, cada una con sus recetas.</summary>
        List<HistorialClinico> ObtenerPorExpediente(string codigoSerologico);

        /// <summary>
        /// Guarda la consulta y sus recetas en UNA transaccion: o se guarda
        /// todo o no se guarda nada (RF-CORE-03). Llena los Id de la
        /// consulta y de cada receta. Debe devolver false (sin lanzar
        /// excepcion) cuando la base lo rechaza por una clave foranea, por
        /// ejemplo un medicamento que no existe.
        /// </summary>
        bool RegistrarConsulta(HistorialClinico consulta);
    }
}
