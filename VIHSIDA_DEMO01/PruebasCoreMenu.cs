using System;
using Conavihsida.Core.Data;
using Conavihsida.Core.Domain.Catalogos;

namespace VIHSIDA_DEMO01
{
    /// <summary>
    /// Menu de prueba manual para los repositorios nuevos del CORE.
    /// Antes de usarlo corre seed_datos_prueba.sql y anota el
    /// PacienteID y MedicoID que te devuelve.
    ///
    /// Para usarlo, agrega esta linea al final de tu Main (Program.cs),
    /// justo antes del ultimo "Console.ReadKey();":
    ///
    ///     PruebasCoreMenu.Ejecutar(ConnectionString);
    ///
    /// (ConnectionString es el mismo campo privado que ya tienes ahi).
    /// </summary>
    internal static class PruebasCoreMenu
    {
        public static void Ejecutar(string connectionString)
        {
            var citas = new CitaRepository(connectionString);
            var facturas = new FacturaRepository(connectionString);
            var resultados = new ResultadoRepository(connectionString);
            var auditoria = new AuditoriaRepository(connectionString);

            bool salir = false;
            while (!salir)
            {
                Console.WriteLine("\n========== PRUEBAS CORE ==========");
                Console.WriteLine("1) Listar medicos");
                Console.WriteLine("2) Agendar una cita de prueba");
                Console.WriteLine("3) Ver citas de un paciente");
                Console.WriteLine("4) Ver facturas de un paciente");
                Console.WriteLine("5) Pagar una factura");
                Console.WriteLine("6) Ver resultados de laboratorio de un paciente");
                Console.WriteLine("0) Salir");
                Console.Write("Opcion: ");
                string opcion = Console.ReadLine();

                try
                {
                    switch (opcion)
                    {
                        case "1":
                            foreach (var m in citas.ObtenerMedicos())
                            {
                                Console.WriteLine($"  MedicoID={m.Id}  {m.NombreCompleto}  ({m.Especialidad})");
                            }
                            break;

                        case "2":
                            Console.Write("MedicoID: ");
                            int medicoId = int.Parse(Console.ReadLine());
                            Console.Write("PacienteID: ");
                            int pacienteId = int.Parse(Console.ReadLine());
                            Console.Write("Fecha (yyyy-MM-dd): ");
                            DateTime fecha = DateTime.Parse(Console.ReadLine());
                            Console.Write("Hora (HH:mm, ej. 09:00): ");
                            string hora = Console.ReadLine();
                            Console.Write("Motivo: ");
                            string motivo = Console.ReadLine();

                            var citaService = new Conavihsida.Core.Aplicacion.Servicios.CitaService(
                                citas, new Conavihsida.Core.Aplicacion.Servicios.AuditoriaService(auditoria));
                            var resultadoCita = citaService.AgendarCita(pacienteId, medicoId, fecha, hora, motivo, "CONSOLA-PRUEBA");

                            if (resultadoCita.Exitoso)
                                Console.WriteLine($"  Cita creada con Id={resultadoCita.Valor.Id}");
                            else
                                Console.WriteLine($"  No se pudo agendar: {resultadoCita.MensajeError}");
                            break;

                        case "3":
                            Console.Write("PacienteID: ");
                            int pIdCitas = int.Parse(Console.ReadLine());
                            foreach (var c in citas.ObtenerCitasPaciente(pIdCitas))
                            {
                                Console.WriteLine($"  CitaID={c.Id}  {c.Fecha:yyyy-MM-dd} {c.Hora}  " +
                                                   $"Dr(a). {c.MedicoNombre}  Estado={c.Estado}");
                            }
                            break;

                        case "4":
                            Console.Write("PacienteID: ");
                            int pIdFact = int.Parse(Console.ReadLine());
                            foreach (var f in facturas.ObtenerPorPaciente(pIdFact))
                            {
                                Console.WriteLine($"  FacturaID={f.Id}  {f.Concepto}  RD$ {f.Monto}  " +
                                                   $"Estado={f.Estado}  Recibo={f.NumeroRecibo ?? "(sin pagar)"}");
                            }
                            break;

                        case "5":
                            Console.Write("PacienteID: ");
                            int pIdPago = int.Parse(Console.ReadLine());
                            Console.Write("FacturaID: ");
                            int facturaId = int.Parse(Console.ReadLine());
                            Console.Write("Forma de pago (Tarjeta/Efectivo/Transferencia): ");
                            string formaPago = Console.ReadLine();

                            var facturacionService = new Conavihsida.Core.Aplicacion.Servicios.FacturacionService(
                                facturas, new Conavihsida.Core.Aplicacion.Servicios.AuditoriaService(auditoria));

                            string referencia = formaPago == FormasPago.Tarjeta ? "REF-CONSOLA-0001" : null;
                            var resultadoPago = facturacionService.PagarFactura(
                                pIdPago, facturaId, formaPago, referencia, cajeroId: null, terminal: "CONSOLA-PRUEBA");

                            if (resultadoPago.Exitoso)
                                Console.WriteLine($"  Pagada. Recibo={resultadoPago.Valor.NumeroRecibo}  Factura={resultadoPago.Valor.NumeroFactura}");
                            else
                                Console.WriteLine($"  No se pudo pagar: {resultadoPago.MensajeError}");
                            break;

                        case "6":
                            Console.Write("PacienteID: ");
                            int pIdRes = int.Parse(Console.ReadLine());
                            var resultadoService = new Conavihsida.Core.Aplicacion.Servicios.ResultadoService(resultados, new Conavihsida.Core.Aplicacion.Servicios.AuditoriaService(auditoria));
                            foreach (var r in resultadoService.ObtenerResultadosPaciente(pIdRes, "CONSOLA-PRUEBA"))
                            {
                                Console.WriteLine($"  {r.Prueba}  Estado={r.Estado}  Nota=\"{r.Nota}\"");
                                // Ojo: r NUNCA tiene ValorResultado. Si necesitas confirmar que el
                                // dato clinico sigue ahi, consulta ResultadosLab directo en SSMS,
                                // nunca agregues ese campo a esta pantalla.
                            }
                            break;

                        case "0":
                            salir = true;
                            break;

                        default:
                            Console.WriteLine("Opcion invalida.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  [ERROR] {ex.Message}");
                    Console.ResetColor();
                }
            }
        }
    }
}
