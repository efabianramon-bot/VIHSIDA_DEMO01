using System;
using System.Collections.Generic;
using System.Linq;

namespace Conavihsida.Core.Empleados.Models
{
    // ----- Lo que devuelve la API (api/usuarios) -----

    public class UsuarioAdminApi
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Rol { get; set; }
        public int? FilialId { get; set; }
        public string FilialNombre { get; set; }
        public bool Activo { get; set; }
        public bool Bloqueado { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoAcceso { get; set; }

        public string NombreCompleto
        {
            get { return (Nombres + " " + Apellidos).Trim(); }
        }
    }

    public class FilialApi
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public bool Activa { get; set; }
    }

    // ----- Pantallas de administracion de usuarios -----

    public class UsuariosInicioViewModel
    {
        public List<UsuarioAdminApi> Usuarios { get; set; }

        /// <summary>Id del administrador que esta viendo la lista: no puede desactivarse a si mismo.</summary>
        public int UsuarioActualId { get; set; }
        public string Error { get; set; }
        public string Exito { get; set; }

        public int Activos
        {
            get { return Usuarios.Count(u => u.Activo); }
        }

        public int Inactivos
        {
            get { return Usuarios.Count(u => !u.Activo); }
        }

        public int Bloqueados
        {
            get { return Usuarios.Count(u => u.Bloqueado); }
        }
    }

    /// <summary>Formulario de alta y de edicion. En la edicion, Username es solo lectura.</summary>
    public class UsuarioForm
    {
        public int? Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Nombres { get; set; }
        public string Apellidos { get; set; }
        public string Rol { get; set; }
        public int? FilialId { get; set; }

        /// <summary>Obligatoria al crear; al editar, vacia = no cambiarla.</summary>
        public string Password { get; set; }
    }

    public class UsuarioFormViewModel
    {
        public UsuarioForm Usuario { get; set; }
        public List<string> Roles { get; set; }
        public List<FilialApi> Filiales { get; set; }
        public string Error { get; set; }

        public bool EsNuevo
        {
            get { return Usuario.Id == null; }
        }
    }
}
