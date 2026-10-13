namespace Conavihsida.Core.Domain.Entidades
{
    /// <summary>
    /// Lo que el CORE responde cuando un paciente inicia sesion. Solo su
    /// identidad civil: nunca el codigo serologico ni datos clinicos (RN-01).
    /// El Gateway arma el token con el PacienteId.
    /// </summary>
    public class PacienteAutenticado
    {
        public int PacienteId { get; set; }
        public string NombreCompleto { get; set; }

        /// <summary>El mismo nombre, con el nombre de campo que espera el Portal Web.</summary>
        public string NombrePaciente
        {
            get { return NombreCompleto; }
        }

        public string Correo { get; set; }
    }
}
