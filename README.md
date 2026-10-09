# VIHSIDA_DEMO01

Solución .NET Framework 4.8 del CORE de Conavihsida: una Web API para citas,
facturas y resultados de laboratorio de pacientes, más una aplicación de
consola para pruebas manuales.

## Proyectos

| Proyecto | Tipo | Contenido |
| --- | --- | --- |
| `Conavihsida.Core.Api` | ASP.NET Web API 2 | Controladores, DTOs y handlers (`ApiKeyHandler`, `LoggingHandler`). |
| `Conavihsida.Core.Aplicacion` | Biblioteca | Servicios (`CitaService`, `FacturacionService`, `ResultadoService`, `AuditoriaService`) y `ResultadoOperacion<T>`. |
| `Conavihsida.Core.Domain` | Biblioteca | Entidades, catálogos, vistas e interfaces de repositorio. |
| `Conavihsida.Core.Data` | Biblioteca | Repositorios sobre SQL Server. |
| `VIHSIDA_DEMO01` | Consola | Programa de demostración y menú de pruebas (`PruebasCoreMenu`). |

La carpeta `UsuarioRepository` contiene un proyecto aparte que no forma parte
de `VIHSIDA_DEMO01.sln`.

## Cómo compilar

1. Abre `VIHSIDA_DEMO01.sln` en Visual Studio (con la carga de trabajo de
   desarrollo web y .NET Framework 4.8).
2. Restaura los paquetes NuGet (la carpeta `packages/` no se versiona).
3. Compila la solución y ejecuta `Conavihsida.Core.Api` para levantar la API.

## Configuración

La API lee dos valores de `Conavihsida.Core.Api/Web.config`:

- Cadena de conexión `ConavihsidaCoreDb`: base de datos SQL Server del CORE.
- Ajuste `CoreApi:ApiKey`: llave compartida que debe enviar quien llame a la API.

## Endpoints

Todas las peticiones deben incluir el header `X-Api-Key`; sin él la API
responde `401`. Las respuestas son JSON en camelCase.

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/api/medicos` | Lista los médicos. |
| GET | `/api/medicos/{medicoId}/horarios?fecha=` | Horarios disponibles de un médico en una fecha. |
| GET | `/api/pacientes/{pacienteId}/citas` | Citas del paciente. |
| POST | `/api/citas` | Agenda una cita (`409` si no se puede agendar). |
| POST | `/api/citas/{citaId}/cancelar?pacienteId=` | Cancela una cita (`404` si no existe). |
| GET | `/api/pacientes/{pacienteId}/facturas` | Facturas del paciente. |
| GET | `/api/pacientes/{pacienteId}/facturas/{facturaId}` | Detalle de una factura. |
| POST | `/api/pacientes/{pacienteId}/facturas/{facturaId}/pagar` | Paga una factura (`409` si no se puede pagar). |
| GET | `/api/pacientes/{pacienteId}/resultados` | Resultados de laboratorio del paciente. |
