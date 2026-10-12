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
rol Médico hay dos, `medico.prueba` y `medico2.prueba`, con la misma
contraseña; los deja listos `Database/Datos/medico_prueba.sql`.

Portales disponibles:

- **Portal Médico** (rol Médico): busca por cédula o por código serológico,
  abre el expediente de un paciente que no lo tiene, muestra el historial con
  carga viral, CD4 y recetas, y registra consultas con sus recetas TARV.
- **Farmacia** (rol Farmacéutico): cola de recetas por despachar, lotes con
  alertas (vencido, por vencer, agotado, bajo mínimo), registro de lotes y
  despacho de recetas indicando de qué lote sale cada unidad.
- **Dashboard Ejecutivo** (rol Administrador): pacientes activos, adherencia
  al tratamiento, inventario crítico y ejecución de subsidios, con gráficos
  y para periodos de 30, 90, 180 o 365 días. Solo muestra totales.
- **Bitácora de auditoría** (roles Auditor y Administrador): consulta de la
  bitácora con filtros por fecha, acción y usuario. Es de solo lectura.

Los gráficos se dibujan con SVG y CSS propios: la aplicación no carga
ninguna librería ni recurso externo.

## Base de datos

La base es el catálogo `ConavihsidaCoreDb` de LocalDB. Sus archivos están en
`Conavihsida.Core.Api/App_Data/` (`ConavihsidaCoreDb.mdf` y
`ConavihsidaCoreDb_log.ldf`). No se versionan, y ninguna compilación ni
limpieza de la solución los toca. La API, la consola y la aplicación de
empleados (a través de la API) usan esa misma base, por nombre de catálogo.

**Respaldos.** Antes de cambiar la estructura o mover archivos, ejecuta:

```
.\Database\respaldar_base.ps1
```

Deja un `.bak` con fecha en `Database/Respaldos/` (tampoco se versiona). El
propio script explica cómo restaurarlo.

`VIHSIDA_DEMO01/Database1.mdf` es una copia antigua, de referencia: tiene la
estructura y los datos originales, sin la migración ni los datos de prueba.

Para crear la base desde cero, ejecuta en este orden:

1. `Database/ConavihsidaCoreDb_schema.sql` (estructura).
2. Los scripts de `Database/Migraciones/`, por número: `001` (roles y
   restricciones) y `002` (cédula del paciente obligatoria, única y con el
   formato `000-0000000-0`) `003` (bloqueo temporal del login del paciente) y `004` (tablas para la
   sincronización de Caja).
3. Los scripts de `Database/Datos/` (solo desarrollo y demo), en este orden:
   `usuarios_prueba`, `medico_prueba`, `paciente_prueba`, `medicamentos_prueba`,
   `facturas_subsidiadas_prueba` y `caja_prueba`.

`caja_prueba.sql` crea los pacientes para las pruebas con Caja, que identifica
al paciente por su cédula:

| Cédula | Caso |
| --- | --- |
| `000-0000004-0` | Expediente activo y una receta activa. |
| `000-0000005-0` | Expediente inactivo. |
| `000-0000006-0` | Paciente sin expediente. |
| `999-9999999-9` | No existe. |

Las recetas vencen a los 30 días: vuelve a ejecutar `caja_prueba.sql` antes de
una prueba para que el primer paciente tenga una receta activa.

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

## Pruebas E2E con el equipo

`docs/GUION_E2E.md` reúne el contrato entre el Gateway y el CORE, el guion
paso a paso para recorrer los criterios de aceptación y lo que todavía falta
acordar con Integración y Caja.

## Pruebas con Postman

`Postman/Conavihsida.Core.Api.postman_collection.json` contiene 94 peticiones
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
| POST | `/api/auth/paciente/login` | Login del paciente del Portal Web con `correo` y `password`; devuelve `pacienteId`, `nombreCompleto` y `correo`, sin token (`401` si no son válidos, `429` si la cuenta está bloqueada 15 minutos tras 5 intentos fallidos). |
| GET | `/api/auth/yo` | Devuelve la identidad de empleado recibida en los encabezados (`401` si falta). |
| GET | `/api/clinica/expedientes` | Lista de expedientes (código, estado y apertura). Solo rol Médico. |
| GET | `/api/clinica/pacientes?cedula=` o `?pacienteId=` | Identifica a un paciente y dice si tiene expediente. Es el único cruce entre identidad y código serológico; solo rol Médico y siempre auditado. |
| GET | `/api/clinica/expediente/{codigo}` | Expediente clínico por código serológico. Solo rol Médico (`403` para los demás). |
| POST | `/api/clinica/expediente` | Abre el expediente de un paciente y genera su código serológico. Si no se indica `medicoResponsableId`, queda como responsable el médico que lo abre. Solo rol Médico. |
| PUT | `/api/clinica/expediente/{codigo}` | Cambia el médico responsable y el estado. Solo rol Médico. |
| GET | `/api/clinica/expediente/{codigo}/historial` | Consultas del expediente con carga viral, CD4 y recetas. Solo rol Médico. |
| POST | `/api/clinica/expediente/{codigo}/consultas` | Registra una consulta y sus recetas TARV en una sola transacción. Solo rol Médico. |
| GET | `/api/clinica/expediente/{codigo}/resultados` | Resultados de laboratorio del expediente, con su valor. Solo rol Médico. |
| POST | `/api/clinica/expediente/{codigo}/resultados` | Solicita una prueba; nace `Pendiente` y el paciente ya la ve. Solo rol Médico. |
| PUT | `/api/clinica/resultados/{resultadoId}` | Publica el valor y deja el resultado `Listo` (`409` si ya estaba publicado). Solo rol Médico. |
| GET | `/api/farmacia/medicamentos` | Catálogo de medicamentos. Roles Farmacéutico, Médico y Administrador. |
| GET | `/api/farmacia/lotes?medicamentoId=` | Lotes con caducidad, existencias y avisos de vencido o bajo mínimo. Roles Farmacéutico y Administrador. |
| POST | `/api/farmacia/lotes` | Registra un lote y su entrada de inventario. Solo rol Farmacéutico. |
| GET | `/api/farmacia/recetas?estado=` | Recetas en un estado; sin `estado`, las Activas (la cola por despachar). Una receta Activa pasa a `Vencida` a los 30 días de emitida si no se despachó. Solo rol Farmacéutico. |
| GET | `/api/farmacia/recetas/{recetaId}` | Consulta una receta antes de despacharla. `descargadaEnCaja` y `fechaDescargo` dicen si Caja ya registró la entrega (una transacción `Completada` con ese `numeroReceta`). Solo rol Farmacéutico. |
| POST | `/api/farmacia/recetas/{recetaId}/despachar` | Despacha una receta activa y descuenta el stock (`409` si no está activa o no hay existencia). La falta de descargo en Caja no lo impide, pero queda en la bitácora como `DespachoSinDescargo`. Solo rol Farmacéutico. |
| GET | `/api/auditoria?desde=&hasta=&usuarioId=&accion=&maximo=` | Bitácora de operaciones, lo más reciente primero (100 por defecto, máximo 500). Roles Auditor y Administrador. |
| GET | `/api/dashboard?dias=&diasAviso=` | Los cuatro indicadores del Dashboard Ejecutivo en una llamada. Solo rol Administrador. |
| GET | `/api/dashboard/pacientes-activos?dias=` | Expedientes activos, inactivos, transferidos y nuevos en el periodo. |
| GET | `/api/dashboard/adherencia?dias=` | Recetas emitidas, retiradas y pendientes, con el porcentaje de adherencia. |
| GET | `/api/dashboard/inventario-critico?diasAviso=` | Lotes vencidos con existencia, por vencer y bajo mínimo. |
| GET | `/api/dashboard/subsidios?dias=` | Facturas subsidiadas, descargadas y entregas ARV. |
| GET | `/api/caja/subsidio?cedula=` | Para Caja: dice si el paciente existe y si tiene subsidio (`existe`, `tieneSubsidio`, `porcentaje`, `vigenteHasta`). Expediente activo = 100 %. Roles Cajero y Administrador. |
| POST | `/api/caja/sync/turnos`, `/movimientos`, `/transacciones`, `/comprobantes`, `/fondos` | Reciben lo que Caja sincroniza y lo guardan en las tablas `Caja*`. Cuerpo y respuesta (`exito`, `mensaje`) según el contrato de Caja. Reenviar un registro lo actualiza por su número de negocio, sin duplicarlo; un dato inválido responde `400`. Roles Cajero y Administrador. |
| GET | `/api/medicos` | Lista los médicos. |
| GET | `/api/medicos/{medicoId}/horarios?fecha=` | Horarios disponibles de un médico en una fecha. |
| GET | `/api/pacientes/{pacienteId}/citas` | Citas del paciente. |
| POST | `/api/citas` | Agenda una cita (`409` si no se puede agendar). |
| POST | `/api/citas/{citaId}/cancelar?pacienteId=` | Cancela una cita (`404` si no existe). |
| GET | `/api/pacientes/{pacienteId}/facturas` | Facturas del paciente. |
| GET | `/api/pacientes/{pacienteId}/facturas/{facturaId}` | Detalle de una factura. |
| POST | `/api/pacientes/{pacienteId}/facturas/{facturaId}/pagar` | Paga una factura (`409` si no se puede pagar). |
| GET | `/api/pacientes/{pacienteId}/resultados` | Resultados de laboratorio del paciente: prueba, fecha, estado y nota, nunca el valor (RN-01). |
