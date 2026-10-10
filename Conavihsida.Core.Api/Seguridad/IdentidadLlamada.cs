using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace Conavihsida.Core.Api.Seguridad
{
    /// <summary>
    /// Quien esta detras de la peticion, segun los encabezados que agrega
    /// el Gateway despues de validar el JWT (contrato acordado con
    /// Integracion): X-Usuario-Id y X-Roles para empleados, X-Paciente-Id
    /// para pacientes del Portal Web. Se confia en ellos porque la
    /// peticion ya paso por ApiKeyHandler: solo el Gateway tiene la llave.
    /// </summary>
    public class IdentidadLlamada
    {
        public const string HeaderUsuarioId = "X-Usuario-Id";
        public const string HeaderRoles = "X-Roles";
        public const string HeaderPacienteId = "X-Paciente-Id";

        public int? UsuarioId { get; private set; }
        public int? PacienteId { get; private set; }
        public IList<string> Roles { get; private set; }

        public bool TieneRol(params string[] rolesPermitidos)
        {
            return Roles.Any(r => rolesPermitidos.Contains(r, StringComparer.OrdinalIgnoreCase));
        }

        public static IdentidadLlamada Leer(HttpRequestMessage request)
        {
            // Varios roles llegan separados por coma: "Medico,Administrador".
            var roles = (LeerHeader(request, HeaderRoles) ?? string.Empty)
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .Where(r => r.Length > 0)
                .ToList();

            return new IdentidadLlamada
            {
                UsuarioId = LeerEntero(request, HeaderUsuarioId),
                PacienteId = LeerEntero(request, HeaderPacienteId),
                Roles = roles
            };
        }

        private static string LeerHeader(HttpRequestMessage request, string nombre)
        {
            IEnumerable<string> valores;
            return request.Headers.TryGetValues(nombre, out valores) ? valores.FirstOrDefault() : null;
        }

        private static int? LeerEntero(HttpRequestMessage request, string nombre)
        {
            int valor;
            return int.TryParse(LeerHeader(request, nombre), out valor) && valor > 0 ? valor : (int?)null;
        }
    }
}
