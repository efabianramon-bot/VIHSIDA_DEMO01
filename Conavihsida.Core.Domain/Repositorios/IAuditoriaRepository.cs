using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public interface IAuditoriaRepository
    {
        void Registrar(RegistroAuditoria registro);
    }
}
