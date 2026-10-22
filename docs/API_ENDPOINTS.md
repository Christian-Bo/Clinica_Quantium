# Referencia de la API — Clínica Quantium

Catálogo de los endpoints HTTP del backend, con el rol autorizado, el cuerpo de la petición, la respuesta y los errores posibles.

**Base en desarrollo:** `https://localhost:7041`
**Documentación interactiva:** `https://localhost:7041/swagger`
**Elaborado por:** Allan Ricardo Quiej Ixmatá
**Rama de referencia:** `develop` @ `43d8791`

---

## 1. Convenciones generales

### Autenticación

Todos los endpoints exigen un token JWT en el encabezado `Authorization`, salvo los marcados como anónimos.

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...
```

El token se obtiene en `POST /api/auth/login` y lleva dentro el identificador del usuario como `NameIdentifier` y un claim por cada rol.

> **En Swagger:** al pulsar **Authorize** hay que pegar **solo el token**, sin la palabra `Bearer`. Swagger la agrega sola; si se escribe, el encabezado queda `Bearer Bearer ...` y todas las llamadas responden 401.

### Formato de fechas

Hay dos tipos de fecha en la API y se comportan distinto.

| Campo | Formato | Zona |
|---|---|---|
| `FechaHoraInicio`, `FechaHoraFin` de una cita | `2026-10-27T09:00:00` | **Hora de Guatemala**, sin la `Z` final |
| `fecha` en el parámetro de disponibilidad | `2026-10-27` | Fecha local |
| `CreatedAtUtc`, `FechaCambioUtc`, `FechaRegistroUtc` | `2026-10-27T15:00:00Z` | UTC |

Enviar `2026-10-27T09:00:00Z` para una cita la correría seis horas. El campo no es UTC.

### Códigos de respuesta

| Código | Cuándo |
|---|---|
| `200 OK` | Operación exitosa con cuerpo |
| `201 Created` | Recurso creado. El encabezado `Location` apunta al recurso |
| `204 No Content` | Operación exitosa sin cuerpo, típicamente una baja |
| `400 Bad Request` | Violación de una regla de negocio. El cuerpo es `{ "error": "mensaje legible" }` |
| `401 Unauthorized` | Falta el token o está vencido |
| `403 Forbidden` | El token es válido pero el rol no alcanza, o el recurso pertenece a otro usuario |
| `404 Not Found` | El recurso no existe |

### Cómo se produce un 400

Todas las violaciones de regla de negocio terminan en el mismo lugar. `DomainException` se lanza en el dominio o en un servicio, o bien `SqlServerExceptionMapper` convierte un error de SQL Server en `DomainException`, y `DomainExceptionHandler` la traduce a un 400 con el cuerpo `ErrorResponse`.

Eso significa que el cliente **nunca ve un error SQL crudo**: recibe el mensaje en español que escribió el trigger o el dominio.

---

## 2. Autenticación — `api/auth`

### `POST /api/auth/login`

Anónimo.

```json
{ "email": "admin@clinica.com", "password": "Admin123!" }
```

Respuesta `200`:

```json
{
  "accessToken": "eyJ...",
  "expiresAtUtc": "2026-10-22T20:00:00Z",
  "usuarioId": "30000000-0000-0000-0000-000000000001",
  "email": "admin@clinica.com",
  "roles": ["Administrador"],
  "mustChangePassword": false,
  "pacienteId": null,
  "nombreCompleto": "Juan Pérez"
}
```

`nombreCompleto` se resuelve por rol: primero busca perfil de paciente, luego de médico, y si no hay ninguno devuelve `null`. Se agregó en el PR #34.

Errores: `400` con credenciales incorrectas, o si el usuario está inactivo o bloqueado por intentos fallidos.

### `POST /api/auth/register/paciente`

Anónimo. Registra un paciente **y su primera cita en la misma operación**.

```json
{
  "email": "paciente@correo.com",
  "password": "Clave123!",
  "nombres": "Ana",
  "apellidos": "López",
  "documento": "1234567890101",
  "telefono": "50001111",
  "direccion": "Zona 1",
  "fechaNacimiento": "1990-05-12",
  "sexo": "F",
  "alergias": "Penicilina",
  "contactoEmergenciaNombre": "Luis López",
  "contactoEmergenciaTelefono": "50002222",
  "medicoId": "...",
  "fechaHoraInicio": "2026-10-27T09:00:00",
  "motivoConsulta": "Control general"
}
```

Los tres últimos campos son obligatorios en la práctica: hay pruebas de integración que verifican que sin ellos la respuesta es `400`. Es una decisión de negocio, no un descuido — el registro existe para agendar.

`sexo` solo acepta `M`, `F` o `X`, y se guarda en mayúscula.

### `POST /api/auth/forgot-password` y `POST /api/auth/reset-password`

Anónimos. El primero encola el correo con el token de recuperación; el segundo lo consume.

### `POST /api/auth/change-password`

Autenticado, cualquier rol.

```json
{ "currentPassword": "Admin123!", "newPassword": "NuevaClave456!" }
```

Devuelve un `AuthResponse` nuevo, porque cambiar la contraseña rota el `SecurityStamp` e invalida el token anterior.

### `GET /api/auth/me`

Autenticado. Devuelve `UsuarioActualDto` con el usuario, sus roles y su nombre.

---

## 3. Citas — `api/citas`

Es el controlador principal. Recibe 18 servicios por constructor y no contiene lógica de negocio: cada acción extrae el usuario del token, llama al servicio y mapea el resultado a DTO.

### Crear

| Ruta | Rol | Descripción |
|---|---|---|
| `POST /api/citas` | Paciente | Solicita su propia cita |
| `POST /api/citas/para-paciente` | Secretaria, Administrador | Crea la cita de un paciente desde mostrador |

Cuerpo del primero:

```json
{
  "medicoId": "...",
  "fechaHoraInicio": "2026-10-27T09:00:00",
  "motivoConsulta": "Dolor de cabeza persistente"
}
```

El segundo agrega `pacienteId`. Ambos responden `201` con `CitaDto` y la cita queda en estado `Solicitada`.

**Validaciones que se aplican, en orden:**

1. Dominio: el motivo tiene entre 5 y 500 caracteres; la cita empieza y termina el mismo día; la fecha es futura.
2. Servicio: el médico existe y está activo; el paciente no tiene otra cita solapada; el paciente no supera 3 citas activas futuras.
3. Base: trigger 51000 a 51006, más el índice único `UX_Citas_Medico_HorarioActivo`.

La duración no se envía: sale del parámetro `Citas.DuracionPredeterminadaMinutos`, que vale 30.

### Consultar

| Ruta | Rol | Devuelve |
|---|---|---|
| `GET /api/citas/mias` | Paciente | Sus citas, más recientes primero |
| `GET /api/citas/medico` | Medico | Las citas asignadas a él |
| `GET /api/citas/pendientes` | Secretaria, Administrador | La cola de citas en estado `Solicitada` |
| `GET /api/citas?pacienteId={guid}` | Secretaria, Administrador | Citas de un paciente. `404` si el paciente no existe |
| `GET /api/citas/paciente/{pacienteId}/historial-medico` | Medico | Contexto clínico: última atención, próximas y pasadas |

### `GET /api/citas/agenda`

Secretaria, Administrador, Medico.

| Parámetro | Tipo | Por omisión |
|---|---|---|
| `desde` | `DateTime?` | Hoy |
| `hasta` | `DateTime?` | `desde` más 7 días |
| `medicoId` | `Guid?` | Todos |
| `estado` | `string?` | Todos |

El parámetro `estado` se agregó en el PR #34 y el filtro baja hasta `ListarAgendaService` y `CitaRepository`. Omitirlo significa "todos los estados".

**Comportamiento especial:** si quien llama tiene rol `Medico` y **no** tiene además `Secretaria` ni `Administrador`, el servicio ignora el `medicoId` recibido y lo fuerza a su propia agenda. Un médico no puede ver la agenda de otro.

Error `400` si el rango es inválido, es decir si `hasta <= desde`.

### `GET /api/citas/disponibilidad`

**Anónimo.** Es el único endpoint de citas sin autenticación.

```
GET /api/citas/disponibilidad?fecha=2026-10-27
```

Devuelve la lista de cupos libres de **todos** los médicos activos para ese día:

```json
[
  {
    "fechaHoraInicio": "2026-10-27T08:00:00",
    "fechaHoraFin": "2026-10-27T08:30:00",
    "medicoId": "...",
    "medicoNombre": "Carlos Méndez"
  }
]
```

El cálculo vive en `ListarDisponibilidadService.cs`: recorre los médicos activos, toma los horarios que apliquen a ese día de la semana y estén vigentes, parte cada bloque en tramos de la duración configurada, y descarta los que choquen con una cita que bloquee agenda o que ya hayan pasado. Ordena por hora y luego por nombre de médico.

Una lista vacía normalmente significa que ningún médico tiene horario configurado para ese día de la semana, no que todo esté ocupado.

> **Nota de seguridad.** Es anónimo a propósito, porque el portal muestra los cupos antes de que el paciente se registre. Expone los huecos de agenda y los nombres de los médicos, pero ningún dato de paciente.

### Transiciones de estado

Cada una corresponde a un método del dominio. El controlador solo decide cuál llamar.

| Ruta | Rol | Transición |
|---|---|---|
| `POST /api/citas/{id}/confirmar` | Secretaria, Administrador | `Solicitada` → `Programada` |
| `POST /api/citas/{id}/rechazar` | Secretaria, Administrador | `Solicitada` → `Rechazada` |
| `POST /api/citas/{id}/anular-solicitud` | Paciente | `Solicitada` → `Rechazada` |
| `POST /api/citas/{id}/confirmar-asistencia` | Paciente | `Programada` → `Confirmada` |
| `POST /api/citas/{id}/cancelar` | Paciente | `Programada` → `Cancelada` o `No presentada` |
| `POST /api/citas/{id}/cancelar-administrativa` | Secretaria, Administrador | `Programada` o `Confirmada` → `Cancelada` |
| `POST /api/citas/{id}/llegada` | Secretaria, Administrador | `Programada` o `Confirmada` → `En Espera` |
| `POST /api/citas/{id}/iniciar` | Medico, Administrador | `En Espera` → `En Atencion` |
| `POST /api/citas/{id}/finalizar` | Medico, Administrador | `En Atencion` → `Atendida` |
| `POST /api/citas/{id}/no-presentada` | Secretaria, Administrador | `Programada`, `Confirmada` o `En Espera` → `No presentada` |

Las que aceptan motivo reciben `{ "motivo": "texto" }`. Si se omite, el sistema usa un motivo por omisión según la operación.

**Tres reglas que no se deducen de la tabla:**

*El paciente no puede cancelar una cita `Confirmada`.* `CancelarPorPaciente` lo bloquea con el mensaje *"Una cita confirmada no puede cancelarse desde el portal del paciente. Comuníquese con la clínica."* La secretaria sí puede, con `cancelar-administrativa`.

*Cancelar tarde no cancela.* `Cancelar` compara contra `Citas.HorasMinimasCancelacion`, que vale 2. Con dos horas o más de anticipación la cita queda `Cancelada`; con menos, queda `No presentada`, que es la que cuenta como inasistencia.

*`iniciar` y `finalizar` verifican asignación.* Si quien llama es un médico que no es el asignado a esa cita, `CitaAccesoMedico.ExigirAsignado` lanza `ForbiddenException` y la respuesta es `403`. Un administrador sí puede.

### `POST /api/citas/{id}/reprogramar`

Secretaria, Administrador.

```json
{ "fechaHoraInicio": "2026-10-28T10:00:00", "motivo": "El paciente lo solicitó" }
```

La cita conserva su estado; solo cambian las fechas y sube `NumeroReprogramaciones`.

Reglas: solo se reprograma desde `Solicitada`, `Programada` o `Confirmada`; el tope es 3; la nueva fecha debe ser distinta de la actual y futura; y **la tercera reprogramación exige autorización de un Administrador**, verificada tanto en el dominio como por el trigger 51004.

### `POST /api/citas/{id}/solicitar-autorizacion-reprogramacion`

Secretaria, Administrador. Encola la solicitud de permiso para la tercera reprogramación. Responde `201` con `AutorizacionReprogramacionDto`.

> **Detalle de diseño:** esta cola **no tiene tabla propia**. Se guarda como filas de `HistorialCitas` con `TipoCambio = 'Autorizacion'`, y `AutorizacionReprogramacionRepository` las reconstruye al leer. En el `DbContext` la entidad está explícitamente ignorada con `modelBuilder.Ignore<AutorizacionReprogramacion>()`. Se hizo así para no pedirle a infraestructura un objeto nuevo en la base compartida.

### `GET /api/citas/{id}/historial`

Autenticado; el servicio filtra según el rol. Devuelve la bitácora completa de la cita, escrita por el trigger `TR_Citas_Historial`.

```json
[
  {
    "historialCitaId": 142,
    "citaId": "...",
    "usuarioId": "...",
    "actorNombre": "María Secretaria",
    "actorRol": "Secretaria",
    "tipoCambio": "CambioEstado",
    "estadoAnterior": "Solicitada",
    "estadoNuevo": "Programada",
    "motivo": "Confirmación administrativa de la solicitud",
    "fechaCambioUtc": "2026-10-22T14:03:00Z",
    "fechaCambioLocal": "2026-10-22T08:03:00",
    "descripcion": "María Secretaria programó la cita"
  }
]
```

`descripcion` la arma `HistorialCitaNarrativa.cs` para que el frontend no tenga que interpretar los códigos.

---

## 4. Preconsulta

### `PUT /api/citas/{citaId}/preconsulta`

Secretaria, Administrador.

```json
{
  "presionSistolicaMmHg": 120,
  "presionDiastolicaMmHg": 80,
  "temperaturaCelsius": 36.6,
  "oxigenoSangrePorcentaje": 98,
  "observacion": "Paciente sin molestias"
}
```

**Es `PUT` y no `POST` a propósito, porque es idempotente.** El servicio busca si ya existe una preconsulta activa para esa cita; si existe la actualiza, si no la crea. Llamarlo dos veces con los mismos datos deja el sistema en el mismo estado, y llamarlo con datos distintos corrige la toma anterior en vez de duplicarla.

**La cita debe estar en `En Espera` o `En Atencion`.** Esta regla se agregó en el PR #39 y la verifica `CitaEstados.PermiteCapturaClinica(estado)`. Intentarlo sobre una cita `Programada` o `Atendida` devuelve `400` con *"Solo se pueden registrar signos vitales cuando la cita está En Espera o En Atencion."* El razonamiento: los signos vitales se toman cuando el paciente está físicamente en la clínica, no antes de que llegue ni después de que se fue.

Rangos validados en el dominio:

| Campo | Mínimo | Máximo |
|---|---|---|
| `presionSistolicaMmHg` | 60 | 250 |
| `presionDiastolicaMmHg` | 40 | 150 |
| `temperaturaCelsius` | 30 | 45 |
| `oxigenoSangrePorcentaje` | 50 | 100 |

Además, la diastólica debe ser estrictamente menor que la sistólica. Cualquier violación devuelve `400` con el mensaje del rango incumplido.

### `GET /api/citas/{citaId}/preconsulta`

Secretaria, Administrador, Medico. Devuelve `PreconsultaDto` o `404` si no hay ninguna activa.

---

## 5. Imágenes de iris

Viven en `ImagenesIrisController.cs`. Las rutas se declaran por método y no a nivel de clase, porque el controlador usa dos prefijos distintos: uno colgado de la cita y otro de la imagen.

### `POST /api/citas/{citaId}/imagenes-iris`

Secretaria, Administrador. `Content-Type: multipart/form-data`. Límite de petición: 12 MB.

| Campo del formulario | Obligatorio | Valores |
|---|---|---|
| `archivo` | Sí | El binario. Máximo 10 MB. Tipos: `image/jpeg`, `image/png`, `image/webp` |
| `lateralidad` | No | `OD`, `OI`, `Ambos` o `No especificada`. Por omisión `No especificada` |
| `observacion` | No | Hasta 500 caracteres |

Responde `201` con `ImagenIrisDto`, donde `tamanoBytes` **viene calculado por SQL Server**, no enviado por el cliente.

Igual que la preconsulta, **la cita debe estar en `En Espera` o `En Atencion`**, verificado con `CitaEstados.PermiteCapturaClinica`. Sobre una cita `Atendida` devuelve `400` con *"Solo se pueden adjuntar imágenes de iris cuando la cita está En Espera o En Atencion."*

> **Dos trampas prácticas.** En Swagger, los campos de texto opcionales traen escrita la palabra `string`; si no se borra, el API recibe `string` como valor real y `lateralidad` falla con `400`. Y la clase del formulario, `SubirImagenIrisForm`, vive en `ClinicaPro.Api` y no en `ClinicaPro.Contracts`, porque usa `IFormFile`, que pertenece a ASP.NET Core, y `Contracts` lo consume también el cliente Blazor.

### `GET /api/citas/{citaId}/imagenes-iris`

Secretaria, Administrador, Medico. Devuelve la lista de metadatos. **Nunca incluye el binario:** el repositorio proyecta a un `record` que no lo contiene.

### `GET /api/imagenes-iris/{id}/archivo`

Secretaria, Administrador, Medico. Devuelve el archivo con su tipo de contenido y nombre original. `404` si no existe.

### `DELETE /api/imagenes-iris/{id}`

Secretaria, Administrador. Baja lógica: pone `IsActive = 0`. Responde `204`. El binario no se borra.

---

## 6. Expediente clínico

Dos endpoints del PR #39 que devuelven el contexto clínico ya armado, para que el frontend no tenga que encadenar varias llamadas.

### `GET /api/citas/{citaId}/expediente`

Secretaria, Administrador, Medico. Devuelve `ExpedienteCitaDto`: la cita, el contexto del paciente, la preconsulta si existe, y la lista de imágenes de iris con la URL de descarga de cada una.

### `GET /api/pacientes/{pacienteId}/expediente`

Secretaria, Administrador, Medico. Devuelve `ExpedientePacienteDto`: el contexto del paciente y el resumen de todas sus visitas, cada una con sus signos vitales y la cantidad de imágenes.

**La regla de acceso** vive en `AccesoExpedienteService.cs`. La secretaria y el administrador pasan directo. Un médico, en cambio, **solo puede abrir el expediente de un paciente con el que tenga al menos una cita**; si no, se lanza `ForbiddenException` y la respuesta es `403`.

Es la única regla de autorización del sistema que depende de los datos y no solo del rol, y por eso no puede resolverse con un atributo `[Authorize]`.

---

## 7. Pacientes — `api/pacientes`

| Ruta | Rol | Descripción |
|---|---|---|
| `GET /api/pacientes/me` | Autenticado | Perfil del paciente autenticado |
| `PUT /api/pacientes/me` | Paciente | Actualiza su propio perfil |
| `GET /api/pacientes?q=&page=&pageSize=` | Secretaria, Administrador | Buscador paginado |
| `POST /api/pacientes` | Secretaria, Administrador | Registra un paciente de mostrador |
| `PUT /api/pacientes/{id}` | Secretaria, Administrador | Actualiza cualquier paciente |
| `DELETE /api/pacientes/{id}` | Administrador | Baja por inasistencia |

El buscador compara el texto contra nombres, apellidos y documento, y devuelve `PaginaPacientesDto` con `items`, `total`, `page` y `pageSize`.

**`DELETE` no borra.** Da de baja lógica, y solo si se cumplen dos condiciones verificadas en `PacienteBajaPorInasistencia.cs`: que la primera cita del paciente haya quedado en `No presentada`, y que nunca haya habido atención clínica. Un paciente que alguna vez fue atendido no puede darse de baja por este camino, porque su expediente tiene valor clínico.

Crear un paciente con un DPI que ya existe devuelve `400` con *"Ya existe un paciente con ese documento"*. Ese mensaje lo produce `SqlServerExceptionMapper` al traducir el error 2601 del índice único filtrado `UQ_Pacientes_Documento`.

---

## 8. Administración — `api/admin`

Todo el controlador exige rol **Administrador**.

### Médicos y horarios

| Ruta | Descripción |
|---|---|
| `GET /api/admin/medicos` | Lista con datos de usuario incluidos |
| `POST /api/admin/medicos` | Crea el usuario y el perfil de médico en una operación |
| `PUT /api/admin/medicos/{id}` | Actualiza datos, estado y opcionalmente correo y contraseña |
| `POST /api/admin/medicos/{id}/horarios` | Agrega un bloque de jornada |
| `PUT /api/admin/medicos/{id}/horarios/{horarioId}` | Modifica un bloque |
| `DELETE /api/admin/horarios/{horarioId}` | Desactiva un bloque |

Cuerpo de un horario:

```json
{
  "diaSemana": 1,
  "horaInicio": "08:00:00",
  "horaFin": "12:00:00",
  "vigenteDesde": null,
  "vigenteHasta": null
}
```

`diaSemana` va de 1 a 7, con **lunes = 1 y domingo = 7**. No es la numeración de `DayOfWeek` de .NET, que empieza en domingo = 0; la conversión la hace `HoraClinica.DiaSemana`.

Un médico sin horarios nunca aparece en `/api/citas/disponibilidad`, aunque esté activo.

### Usuarios y roles

| Ruta | Descripción |
|---|---|
| `GET /api/admin/usuarios` | Lista con sus roles |
| `POST /api/admin/usuarios` | Crea un usuario de personal |
| `PUT /api/admin/usuarios/{id}` | Cambia estado, correo o contraseña |
| `PUT /api/admin/usuarios/{id}/roles` | Reasigna roles, creando la ficha clínica si hace falta |

Las combinaciones de roles permitidas las decide `MatrizCompatibilidadRoles.cs`.

### Autorizaciones de reprogramación

| Ruta | Descripción |
|---|---|
| `GET /api/admin/autorizaciones-reprogramacion?estado=` | Cola de solicitudes |
| `POST /api/admin/autorizaciones-reprogramacion/{id}/aprobar` | Aprueba |
| `POST /api/admin/autorizaciones-reprogramacion/{id}/rechazar` | Rechaza |

### Parámetros y auditoría

| Ruta | Descripción |
|---|---|
| `PUT /api/admin/parametros/{clave}` | Cambia el valor. Solo actualización, no se crean ni se borran claves |
| `GET /api/admin/auditoria` | Bitácora de acciones administrativas |

`Citas.MaximoReprogramaciones` está deliberadamente bloqueado para edición: el tope de 3 es una regla fija del documento de arquitectura, replicada en un `CHECK` de la base.

---

## 9. El resto

| Ruta | Rol | Descripción |
|---|---|---|
| `GET /api/medicos` | **Anónimo** | Médicos activos |
| `GET /api/medicos/{id}` | **Anónimo** | Un médico |
| `GET /api/medicos/{id}/horarios` | **Anónimo** | Sus bloques de jornada |
| `GET /api/parametros` | **Anónimo** | Parámetros activos, para que el frontend conozca las reglas |
| `GET /api/health` | **Anónimo** | Latido del servicio |
| `GET /api/health/database` | **Anónimo** | Verifica la conexión a la base |
| `GET /api/notificaciones/mias` | Paciente | Sus notificaciones |
| `GET /api/notificaciones?estado=&desde=&hasta=` | Secretaria, Administrador | Cola completa |
| `GET /api/reportes/citas?desde=&hasta=&medicoId=` | Secretaria, Administrador | Conteos por estado |
| `POST /api/demo/preparar-agenda` | Administrador | Siembra datos de demostración |

`GET /api/health/database` es la primera llamada que conviene hacer al diagnosticar: si falla, cualquier otro error del sistema es consecuencia de eso.

---

## 10. Tiempo real

El backend expone un *hub* de SignalR en `/hubs/agenda-medico`.

Cuando una secretaria registra la llegada de un paciente con `POST /api/citas/{id}/llegada`, el servicio envía un aviso al médico asignado para que su pantalla se actualice sin recargar.

La autenticación del *hub* es especial: como el navegador no puede poner encabezados en una conexión WebSocket, `Program.cs` configura un evento `OnMessageReceived` que lee el token del parámetro `access_token` de la URL, pero **solo para rutas que empiezan con `/hubs`**.

El envío del aviso está envuelto en `try/catch` y los errores se ignoran deliberadamente: la llegada del paciente ya quedó guardada en la base, y un fallo en la notificación en vivo no debe revertir esa operación.

---

## 11. Patrón de permisos

Leyendo la tabla completa aparece un patrón que conviene saber explicar.

**El paciente** solo puede empujar su cita hacia adelante o anularla antes de que exista compromiso. A partir de `Confirmada`, el control pasa al personal: ya hay un cupo reservado y un médico esperando.

**El médico** solo actúa en la parte clínica del flujo: `iniciar` y `finalizar`. Nunca confirma, rechaza, reprograma ni cancela, porque esas son decisiones administrativas. Y solo sobre sus propias citas.

**La secretaria** controla todo el flujo administrativo, pero no puede autorizar una tercera reprogramación ni administrar usuarios, médicos o parámetros.

**El administrador** puede todo lo de la secretaria, más la autorización de terceras reprogramaciones, la administración del catálogo y la edición de parámetros.

Esa separación es la que hace que la autorización se pueda expresar casi enteramente con atributos `[Authorize(Roles = ...)]`. La única excepción es el expediente clínico del médico, que depende de los datos.
