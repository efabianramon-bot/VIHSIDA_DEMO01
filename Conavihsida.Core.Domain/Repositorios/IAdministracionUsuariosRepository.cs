using System;
using System.Collections.Generic;
using Conavihsida.Core.Domain.Entidades;

namespace Conavihsida.Core.Domain.Repositorios
{
    public enum ResultadoGuardarUsuario
    {
        Guardado,
        NoExiste,
        UsernameRepetido,
        EmailRepetido,

        /// <summary>El rol o la filial indicados no existen.</summary>
        RolOFilialNoExiste
    }

    /// <summary>
    /// Gestion de empleados para el rol Administrador. La contraseña entra
    /// en texto y aqui se guarda solo su hash (BCrypt); nunca sale.
    /// </summary>
    public interface IAdministracionUsuariosRepository
    {
        /// <summary>Todos los usuarios, por apellido y nombre.</summary>
        List<UsuarioAdministrado> Listar();

        /// <summary>null si no existe.</summary>
        UsuarioAdministrado Obtener(int usuarioId);

        List<Filial> ListarFiliales();

        /// <summary>
        /// Crea el usuario con su rol en una sola transaccion y llena
        /// usuario.Id. Si el rol es Medico crea tambien su ficha en Medicos.
        /// </summary>
        ResultadoGuardarUsuario Crear(UsuarioAdministrado usuario, string password, DateTime ahora);

        /// <summary>
        /// Cambia nombres, apellidos, correo, rol y filial. El rol nuevo
        /// reemplaza a los que tuviera. Si password no es null, tambien la
        /// contraseña. El username no se cambia.
        /// </summary>
        ResultadoGuardarUsuario Actualizar(UsuarioAdministrado usuario, string password, DateTime ahora);

        /// <summary>false si el usuario no existe.</summary>
        bool CambiarEstado(int usuarioId, bool activo, DateTime ahora);

        /// <summary>Quita el bloqueo por intentos fallidos. false si el usuario no existe.</summary>
        bool Desbloquear(int usuarioId, DateTime ahora);
    }
}
