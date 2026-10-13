# Guion de pruebas E2E — CORE, Integración, Caja y Web

Preparado por Emilia (CORE) para Ricardo (Integración). Actualizado el 12 de
octubre de 2026.

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
   El paciente entra con `POST /api/auth/paciente/login`.
4. **Caja (Beckham).** Los 7 endpoints que Caja espera de ti ya tienen con
   qué responder desde el CORE: login, subsidio y los cinco de
   sincronización (sección 5).

Lo que necesito que me confirmes está en la sección 7, y la lista completa
de lo que le falta a Integración, en la sección 8.

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
- Este endpoint es solo para **empleados**.

### Login de pacientes

```
POST /api/auth/paciente/login
{ "correo": "paciente.prueba@conavihsida.test", "password": "..." }
```

- `200`: `{"pacienteId":1,"nombreCompleto":"Paciente De Prueba","correo":"paciente.prueba@conavihsida.test"}`.
  Tampoco devuelve token: el Gateway lo genera con el `pacienteId` y, en
  cada petición del paciente, manda `X-Paciente-Id` sacándolo **del token**,
  nunca de lo que envíe el cliente.
- `401`: `{"error":"Credenciales incorrectas."}`. Es el mismo mensaje si el
  correo no existe, si la contraseña está mal o si la cuenta está inactiva.
- `429`: la cuenta quedó bloqueada 15 minutos tras 5 intentos fallidos
  seguidos.
- Pacientes de prueba que pueden entrar: `paciente.prueba@conavihsida.test`
  (n.º 1) y `paciente2.prueba@conavihsida.test` (n.º 2), con la misma
  contraseña de los usuarios de prueba.

### Rutas y quién puede usarlas

| Rutas | Quién |
| --- | --- |
| `/api/clinica/*` (expedientes, historial, consultas, identificar paciente) | Medico |
| `/api/farmacia/medicamentos` | Farmaceutico, Medico, Administrador |
| `/api/farmacia/lotes` (GET) | Farmaceutico, Administrador |
| `/api/farmacia/lotes` (POST), `/api/farmacia/recetas*` | Farmaceutico |
| `/api/dashboard*`, `/api/usuarios*` (administración de empleados) | Administrador |
| `/api/auditoria` | Auditor, Administrador |
| `/api/caja/subsidio?cedula=`, `/api/caja/sync/*` | Cajero, Administrador |
| `/api/pacientes/{id}/citas`, `/facturas`, `/resultados`, `/api/citas*` | El propio paciente (`X-Paciente-Id` igual al id) |
| `/api/medicos`, `/api/medicos/{id}/horarios?fecha=` | Cualquiera con la llave |
| `/api/auth/yo` | Cualquier empleado; devuelve la identidad que recibió el CORE |

La lista completa, con cada ruta y su descripción, está en el `README.md`.
La colección `Postman/Conavihsida.Core.Api.postman_collection.json` trae 109
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

No se puede correr todavía. Depende de las tareas 6 y 8 de la sección 8.

## 5. Lo que Caja espera de Integración y lo que el CORE tiene hoy

El documento de Beckham define 7 endpoints que Caja consume **de
Integración**. Esto es lo que el CORE puede darte para cada uno:

| Endpoint de Caja | Qué necesita | Estado en el CORE |
| --- | --- | --- |
| `POST /auth/login` | Validar credenciales y dar rol y nombre. | **Listo.** Usa `POST /api/auth/login` con `origen: "CAJA"`. Tienes que adaptar la respuesta: Caja espera `exito`, `token`, `rol` (uno solo), `nombreCompleto` y `mensaje`; el CORE devuelve `roles` (lista), `nombres` y `apellidos`. |
| `GET /health` | Saber si Integración está arriba. | No necesita al CORE. |
| `GET /pacientes/{cedula}/subsidio` | Por cédula: si existe, si tiene subsidio, porcentaje y vigencia. | **Listo.** Usa `GET /api/caja/subsidio?cedula=` (roles Cajero o Administrador; acepta la cédula con o sin guiones). Responde `{"existe":true,"tieneSubsidio":true,"porcentaje":100,"vigenteHasta":null}`, la forma que Beckham pidió por mensaje. **Ojo:** el código publicado de Caja (`SubsidioResponseDto`) todavía espera `encontrado`, `tieneSubsidio`, `porcentaje`, `vigente`, `vigenciaHasta` y `mensaje`; mientras Beckham no lo actualice, tienes que traducir los nombres. Regla acordada con Beckham: expediente activo = 100 %, sin vencimiento. Si la cédula no existe responde `200` con `existe: false`; `400` si no tiene 11 dígitos. Pasa la cédula en la consulta, no en la ruta, para que no quede en los logs. |
| `POST /sync/turnos` | Guardar turnos de caja. | **Listo.** `POST /api/caja/sync/turnos`. |
| `POST /sync/movimientos` | Guardar movimientos. | **Listo.** `POST /api/caja/sync/movimientos`. |
| `POST /sync/transacciones` | Guardar cobros y entregas subsidiadas. | **Listo.** `POST /api/caja/sync/transacciones`. Acepta además el campo opcional `numeroReceta`. |
| `POST /sync/comprobantes` | Guardar metadatos de recibos. | **Listo.** `POST /api/caja/sync/comprobantes`. |
| `POST /sync/fondos` | Guardar fondos y convenios. | **Listo.** `POST /api/caja/sync/fondos`. |

Sobre los cinco endpoints de sincronización del CORE:

- Reciben **el mismo cuerpo** que Caja te envía (los `Sync...Dto` de su
  contrato) y responden lo mismo que Caja espera,
  `{"exito":true,"mensaje":"Registro recibido."}`. Puedes reenviar la
  petición y la respuesta sin transformarlas; solo agregas la llave y la
  identidad del cajero (roles Cajero o Administrador).
- Son idempotentes: la clave es el número de negocio (`numeroTurno`,
  `numeroMovimiento`, `numeroTransaccion`, `numeroComprobante`, `codigo`).
  Un registro repetido se actualiza y responde `200` con
  `"Registro actualizado."`. Así funcionan los reintentos, el cierre de un
  turno y la anulación de una transacción.
- Un dato inválido responde `400` con `{"exito":false,"mensaje":"..."}`
  diciendo qué campo corregir. Devuélvelo tal cual: Caja deja el registro
  pendiente.
- `idLocal` se guarda solo como referencia. Los enlaces por
  `turnoCajaIdLocal` y `transaccionIdLocal` solo son fiables con una
  terminal; con más de una habría que enlazar por número de negocio.

Dos consecuencias para el flujo E2E:

- **El descargo de Caja se ve en Farmacia, pero no bloquea el despacho.**
  El criterio 1 dice receta → descargo en Caja → despacho en Farmacia. El
  CORE marca la receta como descargada cuando llega la transacción con su
  `numeroReceta`; el código publicado de Caja todavía no envía ese campo,
  así que hasta que Beckham lo agregue todas las recetas saldrán como
  "pendiente de descargo".
- **El indicador de subsidios del Dashboard ya usa los datos de Caja:**
  entregas subsidiadas, cobros, anulaciones, fondos y convenios, y cuántas
  entregas de Farmacia tienen su descargo. Estará vacío hasta que Caja
  sincronice a través de tu Gateway.

Lo acordado con Beckham para cerrarlo (falta tu confirmación):

1. Caja agrega un campo opcional `numeroReceta` a la transacción subsidiada
   y a `SyncTransaccionDto`. Todavía no está en su código publicado.
2. Los datos de Caja quedan guardados en el CORE, que es la fuente oficial;
   la base local de Caja es solo una cola sin conexión e Integración es el
   paso, no el almacén.
3. Integración recibe cada `/sync/*` de Caja y lo reenvía al endpoint
   equivalente del CORE (tabla de arriba).
4. Al recibir una transacción `Completada` con `numeroReceta`, la receta
   queda descargada. Farmacia la muestra como "pendiente de descargo"
   mientras no llegue. El `numeroReceta` debe ser de una receta del mismo
   paciente de la cédula; si no, el CORE responde `400`.
5. La falta de descargo **no bloquea** el despacho (la terminal de Caja
   puede estar sin conexión): Farmacia puede despachar y queda en la
   bitácora como `DespachoSinDescargo`. El Dashboard muestra cuántas
   entregas se hicieron con descargo y cuántas sin él.
6. Los comprobantes de Caja llevan su propia serie con prefijo de terminal
   (`CJ01-000123`) y no chocan con los `REC-n` / `FAC-n` del CORE.

## 6. Otros pendientes que afectan la integración

- **Login de pacientes.** El CORE ya valida al paciente
  (`POST /api/auth/paciente/login`, sección 3). Falta que el Gateway emita
  el token y que el Portal Web deje de usar su login simulado.
- **CORS.** El ajuste `CoreApi:CorsOrigenes` tiene puertos locales de
  relleno. Si el navegador solo habla con el Gateway, el CORE no necesita
  CORS para la web.
- **HTTPS.** Todo corre en `http` local. Para algo más que la demo, la
  llave y las cookies deben viajar por HTTPS.
- **Vencimiento de recetas.** Una receta activa pasa a `Vencida` a los 30
  días de emitida si no se despachó (el SRS no define el plazo; es decisión
  del equipo).

## 7. Lo que necesito que Ricardo confirme

1. ¿En qué dirección y puerto corre tu Gateway, y cuándo puedo apuntar
   pruebas hacia él?
2. ¿Qué claims lleva tu JWT (id de usuario, roles, vencimiento) y cuánto
   dura? Beckham también lo necesita para Caja.
3. ¿Ya envías `X-Usuario-Id`, `X-Roles` y `X-Paciente-Id` con esos nombres
   exactos? ¿Reenvías `X-Forwarded-For`?
4. ¿Aceptas emitir el token del paciente a partir de
   `POST /api/auth/paciente/login` del CORE (sección 3)? Es la propuesta de
   Francisco.
5. ¿Aceptas simular la pasarela de pago (RF-INT-03)? Francisco necesita tu
   ruta y el formato de la respuesta.
6. ¿Estás de acuerdo con lo acordado en la sección 5: reenviar al CORE los
   datos que Caja sincroniza, incluido `numeroReceta`?
7. ¿Cuándo hacemos la sesión conjunta para correr las partes B y C?

## 8. Lista de trabajo de Integración

Lo que le falta al Gateway para dar el proyecto por integrado, en el orden
en que conviene hacerlo. Todas se pueden hacer ya: el CORE tiene lo que
necesita cada una.

| N.º | Tarea | Qué usa del CORE | Estado del CORE |
| --- | --- | --- | --- |
| 1 | **Login de empleados con JWT.** Recibir usuario y contraseña, llamar al CORE y emitir un JWT con el id de usuario, los roles y el vencimiento. Devolver tal cual el `401`. | `POST /api/auth/login` | Listo |
| 2 | **Login de pacientes con JWT.** Igual, con el `pacienteId` dentro del token. Devolver tal cual el `401` y el `429`. | `POST /api/auth/paciente/login` | Listo |
| 3 | **Validar el JWT en cada petición** y rechazar en el Gateway los vencidos o alterados, sin llamar al CORE. | — | No aplica |
| 4 | **Reenviar al CORE** agregando `X-Api-Key` (guardada solo en el servidor), `X-Usuario-Id` y `X-Roles`, o `X-Paciente-Id`, sacados **del token**. Borrar esos encabezados si los manda el cliente. Reenviar la IP del cliente en `X-Forwarded-For`. | Todas las rutas | Listo |
| 5 | **Portal Web.** Exponer bajo una sola URL base las rutas de médicos, horarios, citas, facturas, pago y resultados, sin cambiarlas. Francisco ya las alineó con el CORE. | Rutas de la sección 3 | Listo |
| 6 | **Caja, lo que no es sincronización.** `POST /auth/login` (adaptar la respuesta a `exito`, `token`, `rol`, `nombreCompleto`, `mensaje`), `GET /health` y `GET /pacientes/{cedula}/subsidio` (llamar a `/api/caja/subsidio?cedula=` y traducir los nombres de los campos mientras Caja no cambie su DTO). | `POST /api/auth/login`, `GET /api/caja/subsidio` | Listo |
| 7 | **Pasarela de pago simulada (RF-INT-03).** Un endpoint que reciba el pago con tarjeta del Portal Web y devuelva una referencia. La web manda esa referencia al CORE como `referenciaPasarela`; la tarjeta y el CVV nunca llegan al CORE. | `POST /api/pacientes/{id}/facturas/{facturaId}/pagar` | Listo |
| 8 | **Sincronización de Caja.** Recibir `/sync/turnos`, `/sync/movimientos`, `/sync/transacciones`, `/sync/comprobantes` y `/sync/fondos`, reenviarlos al CORE y responder a Caja `exito` y `mensaje`. | `POST /api/caja/sync/*` (sección 5) | Listo |
| 9 | **Pruebas conjuntas.** Correr las partes B y C de la sección 4 con los tres módulos levantados. | — | Depende de 1 a 8 |

Reparto de lo que falta, para que no quede nada sin dueño:

- **Ricardo (Integración):** las nueve tareas de la tabla.
- **Emilia (CORE):** nada pendiente de construir. Queda acompañar las
  pruebas conjuntas y corregir lo que salga en ellas.
- **Beckham (Caja):** agregar `numeroReceta` a la transacción y a
  `SyncTransaccionDto`, aceptar la lista de roles en el login, actualizar
  `SubsidioResponseDto` a la forma acordada y usar el prefijo de terminal
  en sus comprobantes.
- **Francisco (Web):** cambiar su login simulado por el del Gateway y su
  pasarela simulada por la de Ricardo, y probar contra el Gateway real.
