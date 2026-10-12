using System;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public enum ResultadoLoginPaciente
    {
        Correcto,

        /// <summary>El correo no existe, la contraseña no coincide o la cuenta esta inactiva.</summary>
        CredencialesIncorrectas,

        /// <summary>Demasiados intentos fallidos seguidos; el bloqueo se levanta solo.</summary>
        BloqueadoTemporalmente
    }

    public interface IPacienteRepository
    {
        /// <summary>
        /// Verifica correo y contraseña (BCrypt) y lleva la cuenta de los
        /// intentos fallidos: al llegar a maximoIntentos bloquea la cuenta
        /// durante minutosBloqueo. paciente solo se llena si es Correcto.
        /// </summary>
        ResultadoLoginPaciente ValidarCredenciales(string correo, string password, DateTime ahora,
                                                   int maximoIntentos, int minutosBloqueo,
                                                   out PacienteAutenticado paciente);
    }
}
