using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conavihsida.Core.Data;
using Conavihsida.Core.Models;

namespace VIHSIDA_DEMO01
{
    internal class Program
    {
        // Ajusta tu Connection String según tu servidor local (ej: Server=localhost;Database=CONAVIHSIDA_DB;Integrated Security=True;TrustServerCertificate=True;)
        // Misma base que usa la API (ver Conavihsida.Core.Api/Web.config): se
        // conecta por nombre de catalogo, sin adjuntar ningun archivo .mdf.
        private const string ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;Initial Catalog=ConavihsidaCoreDb;Integrated Security=True;Connect Timeout=30;";

        static void Main(string[] args)
        {

            Console.Title = "CONAVIHSIDA - Core Testing Console";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("==========================================================");
            Console.WriteLine("   CONAVIHSIDA - PRUEBAS DE CORE: AUTENTICACIÓN Y ROLES   ");
            Console.WriteLine("==========================================================\n");
            Console.ResetColor();
            
            var authService = new UsuarioRepository(ConnectionString);

            // 1. Mostrar cómo generar un hash real para guardarlo en la BD
            Console.WriteLine("[INFO] Ejemplo de hash generado para la contraseña 'Admin123$':");
            string hashEjemplo = UsuarioRepository.GenerarHashPassword("Admin123$");
            Console.WriteLine($"Hash BCrypt: {hashEjemplo}\n");

            // 2. Ejecutar prueba interactiva de login
            Console.Write("Ingrese nombre de usuario (ej. admin.intec): ");
            string username = Console.ReadLine();

            Console.Write("Ingrese contraseña: ");
            string password = Console.ReadLine();

            Console.Write("Ingrese origen ('WEB', 'CAJA' o 'CORE'): ");
            string origen = Console.ReadLine();

            Console.WriteLine("\n[PROCESANDO] Consultando procedimientos almacenados en SQL Server...");

            try
            {
                var resultado = authService.Autenticar(username, password, origen, "192.168.1.10");

                if (resultado.Exitoso)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\n[✔] " + resultado.Mensaje);
                    Console.ResetColor();
                    Console.WriteLine($"Usuario: {resultado.Usuario.Nombres} {resultado.Usuario.Apellidos}");
                    Console.WriteLine($"Email:   {resultado.Usuario.Email}");
                    Console.WriteLine("Roles asignados:");
                    foreach (var rol in resultado.Usuario.Roles)
                    {
                        Console.WriteLine($"  - {rol}");
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("\n[X] Error de autenticación: " + resultado.Mensaje);
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"\n[EXCEPCIÓN] No se pudo conectar a la base de datos o falló el SP: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPresione cualquier tecla para salir...");
            PruebasCoreMenu.Ejecutar(ConnectionString);
            Console.ReadKey();
        }
    }
}

