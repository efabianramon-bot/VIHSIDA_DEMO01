using System.Collections.Generic;
using System.Linq;
using Conavihsida.Core.Empleados.Seguridad;

namespace Conavihsida.Core.Empleados.Models
{
    /// <summary>Un portal de la aplicacion y los roles que pueden verlo.</summary>
    public class OpcionMenu
    {
        public string Titulo { get; set; }
        public string Descripcion { get; set; }

        /// <summary>Nombre del controlador MVC del portal.</summary>
        public string Controlador { get; set; }
        public string[] RolesPermitidos { get; set; }

        /// <summary>false mientras la pantalla no este construida: se muestra, pero sin enlace.</summary>
        public bool Disponible { get; set; }
    }

    /// <summary>
    /// El menu se arma segun los roles del empleado (RF-CORE-06, 07 y 08).
    /// Es el unico lugar donde se declara que rol ve que portal en el menu;
    /// cada controlador lo vuelve a exigir con [AutorizarRol].
    /// </summary>
    public static class MenuPrincipal
    {
        private static readonly OpcionMenu[] Opciones =
        {
            new OpcionMenu
            {
                Titulo = "Portal Médico",
                Descripcion = "Expedientes, historial clínico, consultas y recetas.",
                Controlador = "Medico",
                RolesPermitidos = new[] { NombresRol.Medico },
                Disponible = false
            },
            new OpcionMenu
            {
                Titulo = "Farmacia",
                Descripcion = "Lotes, existencias y despacho de recetas.",
                Controlador = "Farmacia",
                RolesPermitidos = new[] { NombresRol.Farmaceutico },
                Disponible = false
            },
            new OpcionMenu
            {
                Titulo = "Dashboard Ejecutivo",
                Descripcion = "Pacientes activos, adherencia, inventario crítico y subsidios.",
                Controlador = "Dashboard",
                RolesPermitidos = new[] { NombresRol.Administrador },
                Disponible = false
            },
            new OpcionMenu
            {
                Titulo = "Bitácora de auditoría",
                Descripcion = "Quién hizo qué, desde dónde y cuándo.",
                Controlador = "Auditoria",
                RolesPermitidos = new[] { NombresRol.Auditor, NombresRol.Administrador },
                Disponible = false
            }
        };

        public static List<OpcionMenu> Para(SesionEmpleado sesion)
        {
            if (sesion == null)
            {
                return new List<OpcionMenu>();
            }

            return Opciones
                .Where(o => o.RolesPermitidos.Any(r => sesion.Roles.Contains(r)))
                .ToList();
        }
    }
}
