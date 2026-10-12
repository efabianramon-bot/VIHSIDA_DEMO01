using System;
using System.Linq;
using Conavihsida.Core.Aplicacion.Comun;
using Conavihsida.Core.Domain.Catalogos;
using Conavihsida.Core.Domain.Entidades;
using Conavihsida.Core.Domain.Repositorios;

namespace Conavihsida.Core.Aplicacion.Servicios
{
    /// <summary>
    /// Recibe lo que el modulo de Caja sincroniza a traves de Integracion y
    /// lo guarda en el CORE, que es la fuente oficial de esos datos.
    /// Respalda /api/caja/sync/*.
    ///
    /// Cada operacion es idempotente: Caja reenvia lo que no sabe si llego,
    /// y vuelve a mandar un turno al cerrarlo o una transaccion al
    /// anularla. Un registro repetido se actualiza, no se duplica. El valor
    /// del resultado es true si el registro era nuevo.
    /// </summary>
    public class SincronizacionCajaService
    {
        // La cedula dominicana tiene 11 digitos (000-0000000-0).
        private const int LargoCedula = 11;
        private const int LargoNumero = 40;
        private const int LargoUsuario = 50;

        private readonly ICajaRepository _caja;
        private readonly AuditoriaService _auditoria;

        public SincronizacionCajaService(ICajaRepository caja, AuditoriaService auditoria)
        {
            _caja = caja;
            _auditoria = auditoria;
        }

        public ResultadoOperacion<bool> RecibirTurno(TurnoCaja turno, int usuarioId, string terminal)
        {
            string error = Texto(turno.NumeroTurno, "numeroTurno", LargoNumero)
                ?? Texto(turno.UsuarioCajero, "usuarioCajero", LargoUsuario)
                ?? Opcional(turno.NombreCajero, "nombreCajero", 200)
                ?? Opcional(turno.Caja, "caja", 50)
                ?? Opcional(turno.Observacion, "observacion", 500)
                ?? Fecha(turno.FechaApertura, "fechaApertura")
                ?? Catalogo(turno.Estado, "estado", CatalogosCaja.EstadosTurno);
            if (error == null && turno.Estado == "Cerrado" && turno.FechaCierre == null)
            {
                error = "Un turno Cerrado debe traer fechaCierre.";
            }
            if (error != null)
            {
                return Fallo(error);
            }

            turno.NumeroTurno = turno.NumeroTurno.Trim();
            bool nuevo = _caja.GuardarTurno(turno, DateTime.Now);
            return Recibido(nuevo, usuarioId, terminal, "CajaTurnos", turno.NumeroTurno);
        }

        public ResultadoOperacion<bool> RecibirMovimiento(MovimientoCaja movimiento, int usuarioId, string terminal)
        {
            string error = Texto(movimiento.NumeroMovimiento, "numeroMovimiento", LargoNumero)
                ?? Texto(movimiento.UsuarioCajero, "usuarioCajero", LargoUsuario)
                ?? Opcional(movimiento.Concepto, "concepto", 300)
                ?? Opcional(movimiento.Referencia, "referencia", LargoNumero)
                ?? Fecha(movimiento.Fecha, "fecha")
                ?? Catalogo(movimiento.Tipo, "tipo", CatalogosCaja.TiposMovimiento);
            if (error != null)
            {
                return Fallo(error);
            }

            movimiento.NumeroMovimiento = movimiento.NumeroMovimiento.Trim();
            bool nuevo = _caja.GuardarMovimiento(movimiento, DateTime.Now);
            return Recibido(nuevo, usuarioId, terminal, "CajaMovimientos", movimiento.NumeroMovimiento);
        }

        public ResultadoOperacion<bool> RecibirTransaccion(TransaccionCaja transaccion, int usuarioId, string terminal)
        {
            string digitos = new string((transaccion.CedulaPaciente ?? string.Empty).Where(char.IsDigit).ToArray());

            string error = Texto(transaccion.NumeroTransaccion, "numeroTransaccion", LargoNumero)
                ?? Texto(transaccion.UsuarioCajero, "usuarioCajero", LargoUsuario)
                ?? Texto(transaccion.TipoServicio, "tipoServicio", 100)
                ?? Fecha(transaccion.Fecha, "fecha")
                ?? Catalogo(transaccion.FormaPago, "formaPago", CatalogosCaja.FormasPago)
                ?? Catalogo(transaccion.Estado, "estado", CatalogosCaja.EstadosTransaccion);
            if (error == null && digitos.Length != LargoCedula)
            {
                error = "cedulaPaciente debe tener 11 digitos.";
            }
            if (error == null && transaccion.Monto < 0)
            {
                error = "monto no puede ser negativo.";
            }
            if (error == null && transaccion.NumeroReceta != null && transaccion.NumeroReceta <= 0)
            {
                error = "numeroReceta no es valido.";
            }
            if (error != null)
            {
                return Fallo(error);
            }

            // La cedula se guarda siempre con el mismo formato que en Pacientes.
            transaccion.CedulaPaciente = digitos.Insert(10, "-").Insert(3, "-");
            transaccion.NumeroTransaccion = transaccion.NumeroTransaccion.Trim();

            bool? nuevo = _caja.GuardarTransaccion(transaccion, DateTime.Now);
            if (nuevo == null)
            {
                return Fallo("numeroReceta no corresponde a ninguna receta.");
            }

            return Recibido(nuevo.Value, usuarioId, terminal, "CajaTransacciones", transaccion.NumeroTransaccion);
        }

        public ResultadoOperacion<bool> RecibirComprobante(ComprobanteCaja comprobante, int usuarioId, string terminal)
        {
            string error = Texto(comprobante.NumeroComprobante, "numeroComprobante", LargoNumero)
                ?? Texto(comprobante.TipoDocumento, "tipoDocumento", 30)
                ?? Fecha(comprobante.FechaGeneracion, "fechaGeneracion");
            if (error == null && comprobante.CantidadReimpresiones < 0)
            {
                error = "cantidadReimpresiones no puede ser negativa.";
            }
            if (error != null)
            {
                return Fallo(error);
            }

            comprobante.NumeroComprobante = comprobante.NumeroComprobante.Trim();
            bool nuevo = _caja.GuardarComprobante(comprobante, DateTime.Now);
            return Recibido(nuevo, usuarioId, terminal, "CajaComprobantes", comprobante.NumeroComprobante);
        }

        public ResultadoOperacion<bool> RecibirFondo(FondoConvenio fondo, int usuarioId, string terminal)
        {
            string error = Texto(fondo.Codigo, "codigo", LargoNumero)
                ?? Texto(fondo.Nombre, "nombre", 200)
                ?? Opcional(fondo.Organismo, "organismo", 200)
                ?? Fecha(fondo.FechaInicio, "fechaInicio")
                ?? Catalogo(fondo.TipoFuente, "tipoFuente", CatalogosCaja.TiposFuente);
            if (error == null && (fondo.MontoAsignado < 0 || fondo.MontoUtilizado < 0))
            {
                error = "montoAsignado y montoUtilizado no pueden ser negativos.";
            }
            if (error != null)
            {
                return Fallo(error);
            }

            fondo.Codigo = fondo.Codigo.Trim();
            bool nuevo = _caja.GuardarFondo(fondo, DateTime.Now);
            return Recibido(nuevo, usuarioId, terminal, "CajaFondos", fondo.Codigo);
        }

        // En la bitacora queda que tabla y que numero de negocio; nunca la
        // cedula del paciente.
        private ResultadoOperacion<bool> Recibido(bool nuevo, int usuarioId, string terminal,
                                                  string tabla, string numero)
        {
            _auditoria.Registrar(usuarioId, null, terminal, "SincronizarCaja", tabla, null,
                string.Format("numero={0};{1}", numero, nuevo ? "nuevo" : "actualizado"));
            return ResultadoOperacion<bool>.Ok(nuevo);
        }

        private static ResultadoOperacion<bool> Fallo(string mensaje)
        {
            return ResultadoOperacion<bool>.Fallo(mensaje);
        }

        // Cada validador devuelve el mensaje de error, o null si el dato es valido.

        private static string Texto(string valor, string campo, int largoMaximo)
        {
            if (string.IsNullOrWhiteSpace(valor))
            {
                return campo + " es obligatorio.";
            }
            return Opcional(valor, campo, largoMaximo);
        }

        private static string Opcional(string valor, string campo, int largoMaximo)
        {
            return valor != null && valor.Trim().Length > largoMaximo
                ? string.Format("{0} admite maximo {1} caracteres.", campo, largoMaximo)
                : null;
        }

        // Una fecha que no vino en el JSON llega como 0001-01-01, que
        // ademas no cabe en una columna DATETIME.
        private static string Fecha(DateTime valor, string campo)
        {
            return valor.Year < 2000 ? campo + " es obligatoria." : null;
        }

        private static string Catalogo(string valor, string campo, string[] permitidos)
        {
            return permitidos.Contains(valor)
                ? null
                : string.Format("{0} debe ser uno de: {1}.", campo, string.Join(", ", permitidos));
        }
    }
}
