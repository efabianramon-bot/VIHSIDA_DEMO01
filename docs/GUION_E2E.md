# Guion de pruebas E2E — CORE, Integración, Caja y Web

Preparado por Emilia (CORE) para Ricardo (Integración). 10 de octubre de 2026.

Sirve para dos cosas: que Integración sepa exactamente qué expone el CORE y
qué espera recibir, y que el equipo pueda recorrer juntos los criterios de
aceptación del SRS v1.2 (sección 5) sin improvisar.

## 1. Resumen para Ricardo

Lo que te toca para que el flujo completo funcione:

1. **Login.** Tu Gateway recibe usuario y contraseña, llama a
   `POST /api/auth/login` del CORE y, si responde `200`, genera el JWT con
   el `id` y los `roles` que devuelve. El CORE no genera ni valida tokens.
2. **Identidad en cada petición.** Después de validar el JWT, reenvías la
   petición al CORE agregando la llave y quién la hace (sección 3).
3. **Portal Web (Francisco).** Las rutas de citas, facturas y resultados
   exigen desde hoy `X-Paciente-Id`. Sin ese encabezado responden `401`.
4. **Caja (Beckham).** De los 7 endpoints que Caja espera de ti, hoy solo
   el login tiene con qué responder desde el CORE. La validación de
   subsidio y la sincronización no tienen contraparte todavía (sección 5).

Lo que necesito que me confirmes está en la sección 7.

## 2. Cómo levantar el CORE

| Proyecto | Dirección local | Para qué |
| --- | --- | --- |
| `Conavihsida.Core.Api` | `http://localhost:58942/` | La API. Es lo único que consume Integración. |
| `Conavihsida.Core.Empleados` | `http://localhost:58950/` | Aplicación de empleados (Médico, Farmacia, Dashboard, Bitácora). |

- En Visual Studio: clic derecho en la solución → *Configure Startup
  Projects* → *Multiple startup projects* → *Start* en los dos.
- Base de datos: LocalDB, catálogo `ConavihsidaCoreDb` (archivos en
  `Conavihsida.Core.Api/App_Data/`). Para crearla desde
  cero: `Database/ConavihsidaCoreDb_schema.sql`, luego `Database/Migraciones/`
  y luego `Database/Datos/`.
- La llave de la API es el ajuste `CoreApi:ApiKey` de
  `Conavihsida.Core.Api/Web.config`. Pídemela por un canal privado; no va en
  este documento.

Usuarios de prueba (la contraseña está anotada en
`Database/Datos/usuarios_prueba.sql`):

| Usuario | Id | Rol |
| --- | --- | --- |
| `medico2.prueba` | 6 | Medico |
| `admin.prueba` | 2 | Administrador |
| `farmacia.prueba` | 3 | Farmaceutico |
| `auditor.prueba` | 4 | Auditor |
| `cajero.prueba` | 5 | Cajero |

`medico.prueba` (id 1) también tiene rol Medico y la misma contraseña.

Pacientes de prueba: n.º 1 (cédula `000-0000001-0`, expediente
`COD-TEST-0001`), n.º 2 (cédula `000-0000002-0`) y n.º 3 (cédula
`000-0000003-0`). Los dos últimos se crean sin expediente: sirven para mostrar
la apertura en la demo. La cédula es obligatoria, única y siempre con el
formato `000-0000000-0`.

Para las pruebas con Caja, `Database/Datos/caja_prueba.sql` crea además:

| Cédula | Caso |
| --- | --- |
| `000-0000004-0` | Expediente activo y una receta activa. |
| `000-0000005-0` | Expediente inactivo. |
| `000-0000006-0` | Paciente sin expediente. |
| `999-9999999-9` | No existe. |

## 3. Contrato entre el Gateway y el CORE

Todas las peticiones al CORE llevan:

| Encabezado | Cuándo | Valor |
| --- | --- | --- |
| `X-Api-Key` | Siempre | La llave compartida. Sin ella: `401`. |
| `X-Usuario-Id` | Empleado autenticado | El `id` que devolvió el login. |
| `X-Roles` | Empleado autenticado | Sus roles, separados por coma: `Medico,Administrador`. |
| `X-Paciente-Id` | Paciente autenticado (Portal Web) | El id del paciente. |
| `X-Forwarded-For` | Recomendado | IP del cliente; queda como "terminal" en la auditoría. |

El CORE confía en estos encabezados **porque** vienen con la llave. Por eso
el Gateway nunca debe copiarlos tal cual de lo que mande el navegador: tiene
que ponerlos él, a partir del JWT ya validado.

Respuestas de seguridad del CORE:

| Situación | Código | Cuerpo |
| --- | --- | --- |
| Falta o es inválida la llave | `401` | `{"error":"Falta o es invalido el header X-Api-Key."}` |
| Ruta de empleados sin `X-Usuario-Id` | `401` | `{"error":"Falta la identidad del usuario (X-Usuario-Id)."}` |
| El rol no tiene permiso | `403` | `{"error":"Tu rol no tiene acceso a este recurso."}` |
| Ruta de paciente sin `X-Paciente-Id` | `401` | `{"error":"Falta la identidad del paciente (X-Paciente-Id)."}` |
| El paciente pide datos de otro | `403` | `{"error":"Solo puedes acceder a tu propia información."}` |
| Regla de negocio no cumplida | `409` | `{"error":"…motivo legible…"}` |
| Petición mal formada | `400` | `{"message":"…motivo legible…"}` |

### Login

```
POST /api/auth/login
X-Api-Key: <llave>
Content-Type: application/json

{"username": "cajero.prueba", "password": "…", "origen": "CAJA"}
```

- `origen`: `WEB`, `CAJA` o `CORE`. Cualquier otro valor: `400`.
- `200`: `{"id":5,"username":"cajero.prueba","nombres":"Cajero","apellidos":"De Prueba","roles":["Cajero"]}`
- `401`: `{"error":"Credenciales incorrectas."}`. También si la cuenta está
  inactiva o bloqueada (se bloquea a los 5 intentos fallidos).
- Este endpoint es solo para **empleados**. El CORE no tiene todavía un
  login de pacientes (ver sección 6).

### Rutas y quién puede usarlas

| Rutas | Quién |
| --- | --- |
| `/api/clinica/*` (expedientes, historial, consultas, identificar paciente) | Medico |
| `/api/farmacia/medicamentos` | Farmaceutico, Medico, Administrador |
| `/api/farmacia/lotes` (GET) | Farmaceutico, Administrador |
| `/api/farmacia/lotes` (POST), `/api/farmacia/recetas*` | Farmaceutico |
| `/api/dashboard*` | Administrador |
| `/api/auditoria` | Auditor, Administrador |
| `/api/pacientes/{id}/citas`, `/facturas`, `/resultados`, `/api/citas*` | El propio paciente (`X-Paciente-Id` igual al id) |
| `/api/medicos`, `/api/medicos/{id}/horarios?fecha=` | Cualquiera con la llave |
| `/api/auth/yo` | Cualquier empleado; devuelve la identidad que recibió el CORE |

La lista completa, con cada ruta y su descripción, está en el `README.md`.
La colección `Postman/Conavihsida.Core.Api.postman_collection.json` trae 64
peticiones con el código esperado de cada una.

## 4. Guion paso a paso

### Parte A — El CORE solo (se puede correr hoy)

Con los dos proyectos levantados. Cada paso indica el criterio de aceptación
del SRS que cubre.

| # | Quién | Qué hacer | Qué debe pasar | Criterio |
| --- | --- | --- | --- | --- |
| 1 | `medico2.prueba` | Entrar a la aplicación de empleados. | El menú solo muestra *Portal Médico*. | — |
| 2 | Médico | Buscar la cédula `000-0000003-0`. | Aparece el paciente con el botón *Abrir expediente*. | 1 |
| 3 | Médico | Pulsar *Abrir expediente*. | Se crea y muestra un código `EXP-…` generado al azar. | 1 |
| 4 | Médico | Registrar una consulta con carga viral, CD4 y una receta de TLD por 30. | "Consulta registrada con 1 receta." La receta aparece *Activa*. Anotar su número. | 1 |
| 5 | — | Descargo del subsidio en Caja. | **Hoy no está enlazado con el CORE** (sección 5). Se muestra en CajaApp por separado. | 1 |
| 6 | `farmacia.prueba` | Entrar. Abrir la receta desde la cola. | Muestra medicamento, cantidad y existencia; no muestra al paciente. | 1 |
| 7 | Farmacéutico | Pulsar *Despachar*. | "Receta despachada: 30 del lote …". La receta pasa a *Dispensada* y la existencia baja al instante. | 1 |
| 8 | Farmacéutico | Intentar despacharla otra vez (recargar y reenviar). | Rechazado: la receta ya no está activa. | 1 |
| 9 | `cajero.prueba` | Entrar a la aplicación de empleados. | No tiene portales. | 5 |
| 10 | Postman | Carpeta *2. Criterio 5*: un Cajero pide un expediente. | `403 Forbidden`. | 5 |
| 11 | `auditor.prueba` | Abrir la *Bitácora*. | Están, con usuario, terminal y hora: `IniciarSesion`, `IdentificarPaciente`, `AbrirExpediente`, `RegistrarConsulta`, `EmitirReceta`, `DespacharReceta` y el `AccesoDenegado` del paso 10. | 6 |
| 12 | `admin.prueba` | Abrir el *Dashboard*. | Pacientes activos, adherencia, inventario crítico y subsidios reflejan lo anterior. | — |
| 13 | — | Abrir `Conavihsida.Core.Api/App_Data/logs/base-datos-FECHA.log`. | Hay un `COMMIT` por la consulta y otro por el despacho. Ninguna línea `LENTA` (más de 500 ms). | 7 |
| 14 | Postman | *Run collection* completa. | 64 de 64 pruebas correctas. | 4, 5 |

Sobre el criterio 4 (validaciones CHECK): el esquema tiene 21 restricciones
CHECK y ningún trigger. En la colección se ven actuar, por ejemplo, al
registrar un lote repetido o una consulta con un medicamento inexistente.

### Parte B — A través del Gateway (cuando esté listo)

Repetir con el Gateway en medio. Lo que hay que comprobar en cada caso:

| # | Prueba | Resultado esperado |
| --- | --- | --- |
| B1 | Login de `cajero.prueba` por el Gateway. | Devuelve un JWT. En el CORE queda una línea en `SesionesLog` con el origen correcto. |
| B2 | `GET /api/auth/yo` con el JWT de un médico. | El CORE responde `{"usuarioId":6,"roles":["Medico"]}`: los encabezados llegaron bien. |
| B3 | `GET /api/clinica/expediente/COD-TEST-0001` con el JWT del cajero. | `403`, y el Gateway lo devuelve tal cual. Criterio 5. |
| B4 | La misma petición con un JWT vencido o alterado. | La rechaza **el Gateway**; al CORE no le llega nada. |
| B5 | Una petición donde el cliente manda él mismo `X-Roles: Medico`. | El Gateway lo ignora y pone los roles del JWT. |
| B6 | Un paciente pide sus propias facturas. | `200`. |
| B7 | Ese mismo paciente pide las facturas de otro. | `403`. |
| B8 | Petición directa al CORE sin pasar por el Gateway y sin la llave. | `401`. |

### Parte C — Con Caja

No se puede correr todavía. Depende de lo que se acuerde en la sección 5.

## 5. Lo que Caja espera de Integración y lo que el CORE tiene hoy

El documento de Beckham define 7 endpoints que Caja consume **de
Integración**. Esto es lo que el CORE puede darte para cada uno:

| Endpoint de Caja | Qué necesita | Estado en el CORE |
| --- | --- | --- |
| `POST /auth/login` | Validar credenciales y dar rol y nombre. | **Listo.** Usa `POST /api/auth/login` con `origen: "CAJA"`. Tienes que adaptar la respuesta: Caja espera `exito`, `token`, `rol` (uno solo), `nombreCompleto` y `mensaje`; el CORE devuelve `roles` (lista), `nombres` y `apellidos`. |
| `GET /health` | Saber si Integración está arriba. | No necesita al CORE. |
| `GET /pacientes/{cedula}/subsidio` | Por cédula: si existe, si tiene subsidio, porcentaje y vigencia. | **No existe.** La tabla `Pacientes` del CORE no guarda subsidio, porcentaje ni vigencia. Hay que acordar la regla (pregunta para Beckham). |
| `POST /sync/turnos` | Guardar turnos de caja. | **No existe.** El CORE no tiene tablas de turnos ni movimientos. |
| `POST /sync/movimientos` | Guardar movimientos. | **No existe.** |
| `POST /sync/transacciones` | Guardar cobros y entregas subsidiadas. | **No existe.** |
| `POST /sync/comprobantes` | Guardar metadatos de recibos. | **No existe.** |
| `POST /sync/fondos` | Guardar fondos y convenios. | **No existe.** |

Dos consecuencias para el flujo E2E:

- **El descargo de Caja no bloquea el despacho.** El criterio 1 dice
  receta → descargo en Caja → despacho en Farmacia, pero el CORE despacha
  cualquier receta activa. Las transacciones de Caja se identifican por la
  cédula del paciente y no traen el número de receta, así que hoy no hay
  cómo enlazarlas.
- **El indicador de subsidios del Dashboard no ve a Caja.** Se calcula con
  las facturas subsidiadas del propio CORE.

Mi propuesta para cerrarlo, a confirmar entre los tres:

1. Caja agrega un campo opcional `numeroReceta` a la transacción subsidiada.
2. Integración, al recibir esa transacción, llama a un endpoint nuevo del
   CORE (por ejemplo `POST /api/caja/descargos`) con el número de receta,
   el número de transacción y el de comprobante.
3. El CORE marca la receta como descargada y Farmacia solo puede despachar
   recetas descargadas.

## 6. Otros pendientes que afectan la integración

- **Login de pacientes.** El Portal Web necesita autenticar pacientes, y el
  CORE solo tiene login de empleados. Hay que decidir quién lo hace.
- **CORS.** El ajuste `CoreApi:CorsOrigenes` tiene puertos locales de
  relleno. Si el navegador solo habla con el Gateway, el CORE no necesita
  CORS para la web.
- **HTTPS.** Todo corre en `http` local. Para algo más que la demo, la
  llave y las cookies deben viajar por HTTPS.
- **Vencimiento de recetas.** Nada pasa una receta al estado `Vencida`; el
  SRS no define la regla.

## 7. Lo que necesito que Ricardo confirme

1. ¿En qué dirección y puerto corre tu Gateway, y cuándo puedo apuntar
   pruebas hacia él?
2. ¿Qué claims lleva tu JWT (id de usuario, roles, vencimiento) y cuánto
   dura? Beckham también lo necesita para Caja.
3. ¿Ya envías `X-Usuario-Id`, `X-Roles` y `X-Paciente-Id` con esos nombres
   exactos? ¿Reenvías `X-Forwarded-For`?
4. ¿Quién autentica a los pacientes del Portal Web: tu Gateway con una
   tabla propia, o necesitas que el CORE exponga un login de pacientes?
5. Los datos que Caja sincroniza (`/sync/*`): ¿los guardas tú en
   Integración, o esperas que el CORE los reciba y los guarde?
6. ¿Estás de acuerdo con la propuesta de la sección 5 para enlazar el
   descargo de Caja con la receta?
7. ¿Cuándo hacemos la sesión conjunta para correr las partes B y C?
