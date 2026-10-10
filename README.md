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
- Ajuste `CoreApi:CorsOrigenes`: orígenes del Portal Web autorizados a llamar a
  la API desde el navegador (CORS), separados por coma.

## Base de datos

Para crear la base desde cero, ejecuta en este orden:

1. `Database/ConavihsidaCoreDb_schema.sql` (estructura).
2. Los scripts de `Database/Migraciones/` (roles y restricciones).
3. Los scripts de `Database/Datos/` (solo desarrollo y demo: usuarios, paciente,
   medicamentos y facturas de prueba).

## Pruebas con Postman

`Postman/Conavihsida.Core.Api.postman_collection.json` contiene 49 peticiones
con sus pruebas, ordenadas según el flujo de la demo. Impórtala en Postman,
llena las variables `apiKey` y `clavePrueba` como indica su descripción y usa
*Run collection* para ejecutarlas todas.

## Endpoints

Todas las peticiones deben incluir el header `X-Api-Key`; sin él la API
responde `401`. Las respuestas son JSON en camelCase.

El JWT lo genera y valida el Gateway. Al reenviar una petición, el Gateway
indica quién la hace con estos encabezados:

- `X-Usuario-Id` y `X-Roles` para empleados. Varios roles van separados por
  coma, por ejemplo `X-Roles: Medico,Administrador`.
- `X-Paciente-Id` para pacientes del Portal Web.

Los endpoints marcados con `[RequiereRol(...)]` responden `401` si falta
`X-Usuario-Id` y `403` si el empleado no tiene alguno de los roles permitidos.

| Método | Ruta | Descripción |
| --- | --- | --- |
| POST | `/api/auth/login` | Valida usuario y contraseña; devuelve el usuario y sus roles, sin token (`401` si no son válidos). |
| GET | `/api/auth/yo` | Devuelve la identidad de empleado recibida en los encabezados (`401` si falta). |
| GET | `/api/clinica/expediente/{codigo}` | Expediente clínico por código serológico. Solo rol Médico (`403` para los demás). |
| POST | `/api/clinica/expediente` | Abre el expediente de un paciente y genera su código serológico. Solo rol Médico. |
| PUT | `/api/clinica/expediente/{codigo}` | Cambia el médico responsable y el estado. Solo rol Médico. |
| GET | `/api/clinica/expediente/{codigo}/historial` | Consultas del expediente con carga viral, CD4 y recetas. Solo rol Médico. |
| POST | `/api/clinica/expediente/{codigo}/consultas` | Registra una consulta y sus recetas TARV en una sola transacción. Solo rol Médico. |
| GET | `/api/farmacia/medicamentos` | Catálogo de medicamentos. Roles Farmacéutico, Médico y Administrador. |
| GET | `/api/farmacia/lotes?medicamentoId=` | Lotes con caducidad, existencias y avisos de vencido o bajo mínimo. Roles Farmacéutico y Administrador. |
| POST | `/api/farmacia/lotes` | Registra un lote y su entrada de inventario. Solo rol Farmacéutico. |
| GET | `/api/farmacia/recetas/{recetaId}` | Consulta una receta antes de despacharla. Solo rol Farmacéutico. |
| POST | `/api/farmacia/recetas/{recetaId}/despachar` | Despacha una receta activa y descuenta el stock (`409` si no está activa o no hay existencia). Solo rol Farmacéutico. |
| GET | `/api/auditoria?desde=&hasta=&usuarioId=&accion=&maximo=` | Bitácora de operaciones, lo más reciente primero (100 por defecto, máximo 500). Roles Auditor y Administrador. |
| GET | `/api/dashboard?dias=&diasAviso=` | Los cuatro indicadores del Dashboard Ejecutivo en una llamada. Solo rol Administrador. |
| GET | `/api/dashboard/pacientes-activos?dias=` | Expedientes activos, inactivos, transferidos y nuevos en el periodo. |
| GET | `/api/dashboard/adherencia?dias=` | Recetas emitidas, retiradas y pendientes, con el porcentaje de adherencia. |
| GET | `/api/dashboard/inventario-critico?diasAviso=` | Lotes vencidos con existencia, por vencer y bajo mínimo. |
| GET | `/api/dashboard/subsidios?dias=` | Facturas subsidiadas, descargadas y entregas ARV. |
| GET | `/api/medicos` | Lista los médicos. |
| GET | `/api/medicos/{medicoId}/horarios?fecha=` | Horarios disponibles de un médico en una fecha. |
| GET | `/api/pacientes/{pacienteId}/citas` | Citas del paciente. |
| POST | `/api/citas` | Agenda una cita (`409` si no se puede agendar). |
| POST | `/api/citas/{citaId}/cancelar?pacienteId=` | Cancela una cita (`404` si no existe). |
| GET | `/api/pacientes/{pacienteId}/facturas` | Facturas del paciente. |
| GET | `/api/pacientes/{pacienteId}/facturas/{facturaId}` | Detalle de una factura. |
| POST | `/api/pacientes/{pacienteId}/facturas/{facturaId}/pagar` | Paga una factura (`409` si no se puede pagar). |
| GET | `/api/pacientes/{pacienteId}/resultados` | Resultados de laboratorio del paciente. |
