using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IAuditoriaRepository
    {
        void Registrar(RegistroAuditoria registro);

        /// <summary>
        /// Registros mas recientes primero. Los filtros en null no se aplican.
        /// </summary>
        List<RegistroAuditoria> Consultar(DateTime? desde, DateTime? hasta, int? usuarioId,
                                          string accion, int maximo);
    }
}
