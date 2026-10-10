# VIHSIDA_DEMO01

Solución .NET Framework 4.8 del módulo CORE del Sistema Integral de
CONAVIHSIDA (SRS v1.2). Es la Web API central del sistema:

- Login de empleados y control de acceso por rol (RF-CORE-01).
- Expediente clínico confidencial, historial y recetas TARV (RF-CORE-02 y 03).
- Inventario farmacéutico y despacho de recetas (RF-CORE-04).
- Auditoría de operaciones y bitácora para el rol Auditor (RF-CORE-05).
- Indicadores del Dashboard Ejecutivo (RF-CORE-08).
- Citas, facturas y resultados para el Portal Web del paciente.

Incluye además una aplicación de consola para pruebas manuales.

## Proyectos

| Proyecto | Tipo | Contenido |
| --- | --- | --- |
| `Conavihsida.Core.Api` | ASP.NET Web API 2 | Controladores, DTOs, handlers (llave de API, CORS, logs) y filtros de seguridad (`RequiereRol`, `SoloElPaciente`). |
| `Conavihsida.Core.Aplicacion` | Biblioteca | Servicios con las reglas de negocio (autenticación, expediente, historial, inventario, dashboard, citas, facturación, auditoría) y `ResultadoOperacion<T>`. |
| `Conavihsida.Core.Domain` | Biblioteca | Entidades, catálogos, vistas e interfaces de repositorio. |
| `Conavihsida.Core.Data` | Biblioteca | Repositorios sobre SQL Server. |
| `Conavihsida.Core.Empleados` | ASP.NET MVC 5 | Aplicación de empleados (RF-CORE-06, 07 y 08): login y portales por rol. No toca la base: todo lo pide a la API. |
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

## Aplicación de empleados

`Conavihsida.Core.Empleados` es la interfaz interna para Médico, Farmacia,
Administrador y Auditor. El empleado entra con su usuario; la aplicación
llama a `POST /api/auth/login` y, desde ahí, envía en cada llamada a la API
la llave, `X-Usuario-Id` y `X-Roles`. El menú muestra solo los portales del
rol, y la API vuelve a comprobar el rol en cada petición.

Para usarla hay que tener corriendo **los dos proyectos**:

1. En Visual Studio, clic derecho en la solución → *Configure Startup
   Projects* → *Multiple startup projects*, y pon en *Start*
   `Conavihsida.Core.Api` y `Conavihsida.Core.Empleados`.
2. La aplicación abre en `http://localhost:58950/` y busca la API en
   `http://localhost:58942/` (ajuste `CoreApi:BaseUrl` de su `Web.config`).
3. El ajuste `CoreApi:ApiKey` de su `Web.config` debe ser igual al de la API.

Los usuarios de prueba están en `Database/Datos/usuarios_prueba.sql`. Para el
rol Médico usa `medico2.prueba` (`Database/Datos/medico_prueba.sql`), con la
misma contraseña: el usuario `medico.prueba` original tiene un hash de relleno
y no puede iniciar sesión.

Portales disponibles:

- **Portal Médico** (rol Médico): busca por cédula o por código serológico,
  abre el expediente de un paciente que no lo tiene, muestra el historial con
  carga viral, CD4 y recetas, y registra consultas con sus recetas TARV.

## Base de datos

Para crear la base desde cero, ejecuta en este orden:

1. `Database/ConavihsidaCoreDb_schema.sql` (estructura).
2. Los scripts de `Database/Migraciones/` (roles y restricciones).
3. Los scripts de `Database/Datos/` (solo desarrollo y demo: usuarios, paciente,
   medicamentos y facturas de prueba).

## Logs

La API escribe tres archivos por día en `Conavihsida.Core.Api/App_Data/logs/`
(la carpeta no se versiona ni se sirve por HTTP). Ninguno guarda el cuerpo de
las peticiones, los parámetros de las consultas ni datos clínicos.

| Archivo | Contenido |
| --- | --- |
| `transacciones-AAAA-MM-DD.log` | Una línea por operación de negocio: usuario, roles o paciente, terminal, operación, ruta, código, duración y si quedó `COMPLETADA`, `RECHAZADA` (con el motivo) o en `ERROR`. |
| `base-datos-AAAA-MM-DD.log` | `ERROR_SQL` y `CONEXION` (errores), `RESTRICCION` (violación de UNIQUE o clave foránea), `LENTA` (más de 500 ms), `COMMIT` y `ROLLBACK`, con el repositorio y método que los originó. |
| `core-api-AAAA-MM-DD.log` | Log técnico: todas las peticiones con su código y duración, y las excepciones. |

Los destinos y formatos se cambian en `Conavihsida.Core.Api/NLog.config`.

## Pruebas con Postman

`Postman/Conavihsida.Core.Api.postman_collection.json` contiene 60 peticiones
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

Los endpoints de citas, facturas y resultados del paciente llevan
`[SoloElPaciente]`: responden `401` si falta `X-Paciente-Id` y `403` si ese
paciente no es el mismo `pacienteId` de la petición. Los catálogos
`/api/medicos` y sus horarios no lo exigen.

| Método | Ruta | Descripción |
| --- | --- | --- |
| POST | `/api/auth/login` | Valida usuario y contraseña; devuelve el usuario y sus roles, sin token (`401` si no son válidos). |
| GET | `/api/auth/yo` | Devuelve la identidad de empleado recibida en los encabezados (`401` si falta). |
| GET | `/api/clinica/expedientes` | Lista de expedientes (código, estado y apertura). Solo rol Médico. |
| GET | `/api/clinica/pacientes?cedula=` o `?pacienteId=` | Identifica a un paciente y dice si tiene expediente. Es el único cruce entre identidad y código serológico; solo rol Médico y siempre auditado. |
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
