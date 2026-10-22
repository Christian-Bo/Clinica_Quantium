# Decisiones de arquitectura — Clínica Quantium

Registro de las decisiones técnicas que tomó el equipo, con el problema que resolvían, las alternativas que se descartaron y las consecuencias que dejaron.

Está escrito en formato de ADR (*Architecture Decision Record*): cada entrada documenta una decisión, no una funcionalidad. La utilidad de este documento no es explicar qué hace el sistema —eso está en `MODELO_DATOS.md` y `API_ENDPOINTS.md`— sino **por qué está hecho así y no de otra forma**.

**Elaborado por:** Allan Ricardo Quiej Ixmatá
**Rama de referencia:** `develop` @ `43d8791`

---

## ADR-01 — Monolito modular en lugar de microservicios

**Estado:** aceptada

**Contexto.** El sistema lo desarrollan cinco personas en un semestre, con un despliegue único y una base de datos compartida en un hosting gratuito.

**Decisión.** Un monolito modular: un solo proceso y un solo despliegue, pero la solución partida en seis proyectos con dependencias en una sola dirección.

**Alternativa descartada: microservicios.** Habría significado al menos tres servicios —agenda, identidad, notificaciones— con sus despliegues, su comunicación por red y su consistencia eventual entre bases separadas. Para un equipo de cinco personas, todo ese costo se paga sin recibir nada a cambio: no hay escalado independiente que necesitar, ni equipos separados que desacoplar.

**Consecuencias.**

A favor: una sola cadena de conexión, una sola transacción cuando se necesita atomicidad, depuración en un solo proceso, y despliegue en un solo paso.

En contra: para escalar hay que replicar todo el sistema, no solo la parte cargada. No es un problema a esta escala.

**Cómo se mantiene la modularidad.** La dirección de las referencias no se invierte nunca: `Domain` no conoce a nadie, `Application` conoce a `Domain`, `Infrastructure` conoce a ambas, `Api` conoce a todas. Si esa regla se rompe, el monolito deja de ser modular y se convierte en un monolito a secas.

---

## ADR-02 — El dominio sin dependencias externas

**Estado:** aceptada, con verificación automática

**Contexto.** Las reglas de negocio del módulo de citas son complejas: una máquina de nueve estados, ventanas de cancelación, topes de reprogramación con autorización. Son las que más cambian y las que más caro sale equivocarse.

**Decisión.** `ClinicaPro.Domain` no referencia Entity Framework, SQL Server, ASP.NET Core ni ninguna otra biblioteca de infraestructura. Solo la biblioteca base de .NET.

**Por qué importa.** El beneficio no es estético. Es que 183 pruebas unitarias corren en 150 milisegundos sin conexión a nada. Una suite que tarda medio segundo se ejecuta en cada guardado; una que tarda un minuto se ejecuta una vez al día y deja de servir como red de seguridad.

**Cómo se protege.** `tests/ClinicaPro.UnitTests/DomainAssemblyTests.cs` verifica que el ensamblado no tenga dependencias prohibidas. Si alguien agrega un paquete de acceso a datos, la prueba falla y el mensaje explica el motivo.

**Consecuencias.** Las entidades no pueden tener propiedades de navegación cargadas por EF Core ni atributos de mapeo. La configuración vive en `Infrastructure/Persistence/Configurations/`, separada de las clases que configura. Es más código y vale la pena.

---

## ADR-03 — Las reglas se validan en tres capas

**Estado:** aceptada

**Contexto.** Una regla como "no puede haber dos citas del mismo médico a la misma hora" se puede validar en el dominio, en el servicio o en la base de datos. Cada lugar atrapa casos distintos.

**Decisión.** Validar en los tres, con un reparto explícito de responsabilidades.

| Capa | Qué puede validar | Qué no puede |
|---|---|---|
| Dominio | Lo que se decide con los datos de la propia entidad: longitud del motivo, mismo día, estado de origen, fecha futura | Nada que requiera consultar la base |
| Aplicación | Lo que necesita otras filas: solape del paciente, tope de citas activas, existencia del médico | Nada contra una condición de carrera: entre su `SELECT` y el `INSERT` otra petición puede colarse |
| Base de datos | Todo, de forma atómica, incluso con dos peticiones en el mismo milisegundo o con alguien escribiendo por SQL directo | Dar un mensaje legible: solo lanza un número |

**La frase que resume el reparto:** el dominio da el mensaje claro, la base da la garantía.

**Alternativa descartada: validar solo en la base.** Es suficiente para la corrección, pero el usuario recibiría errores SQL crudos. Mostrar *"Violation of UNIQUE KEY constraint 'UX_Citas_Medico_HorarioActivo'"* a una secretaria no es aceptable.

**Alternativa descartada: validar solo en el código.** Es suficiente para la experiencia de usuario, pero no para la corrección. Dos clics simultáneos insertan dos citas en el mismo cupo, porque entre la consulta y la inserción hay una ventana.

**Consecuencias.** La misma regla se escribe dos o tres veces y pueden desincronizarse. Se mitiga centralizando lo que se puede: la lista de estados que bloquean agenda vive en la columna calculada `BloqueaHorario` y en `CitaEstados.BloqueaHorario`, no repartida por el código.

---

## ADR-04 — El historial lo escribe un trigger, no la aplicación

**Estado:** aceptada

**Contexto.** Un sistema clínico necesita trazabilidad: quién cambió cada cita, cuándo y por qué. La pregunta es dónde vive esa escritura.

**Decisión.** La escribe el trigger `TR_Citas_Historial` sobre `dbo.Citas`. La aplicación nunca hace `INSERT` en `HistorialCitas`.

**Por qué.** Porque así es inevitable. Si lo escribiera el servicio, cada camino nuevo que modifique una cita tendría que acordarse de registrar, y un `UPDATE` manual desde DBeaver no quedaría registrado nunca. En el trigger no hay forma de saltarlo.

**El problema que hubo que resolver.** Un trigger solo ve las tablas `inserted` y `deleted`, y ninguna trae el usuario de la aplicación: todas las conexiones entran con la misma credencial de SQL Server.

**La solución.** `SESSION_CONTEXT`. La aplicación deja el identificador del usuario y el motivo en la sesión de la conexión, dentro de la misma transacción, inmediatamente antes del `UPDATE`:

```sql
EXEC sys.sp_set_session_context @key = N'UsuarioId',    @value = '<guid>';
EXEC sys.sp_set_session_context @key = N'MotivoCambio', @value = N'<motivo>';
UPDATE dbo.Citas SET ...;
```

El trigger lo recoge con `SESSION_CONTEXT(N'UsuarioId')`. Si no hay contexto en un cambio de estado o de fecha, lanza el error 51006 y aborta la operación completa.

**La consecuencia más importante:** es imposible cambiar una cita sin dejar rastro, ni desde el API, ni desde DBeaver, ni desde una consulta manual.

**Alternativa descartada: un usuario de SQL Server por cada usuario de la aplicación.** Permitiría que el trigger leyera `SUSER_SNAME()`, pero obligaría a crear y mantener credenciales de base por cada persona, y haría imposible el agrupamiento de conexiones.

**Consecuencias negativas.** El contexto vive en la conexión, no en la transacción, así que con un *pool* mal manejado un valor podría filtrarse a otra petición. Se mitiga escribiéndolo siempre inmediatamente antes de cada `SaveChanges`. Y toda escritura que cambie estado debe pasar por `SaveChangesWithSqlSessionContextAsync`: usar `SaveChangesAsync` a secas falla con 51006.

---

## ADR-05 — `BloqueaHorario` como columna calculada persistida

**Estado:** aceptada

**Contexto.** La pregunta *"¿esta cita está ocupando un espacio en la agenda?"* aparece en el trigger de validación, en el índice único, en el cálculo de disponibilidad y en varias consultas de reporte. La respuesta es la misma lista de cinco estados.

**Decisión.** Una columna calculada y `PERSISTED` sobre `dbo.Citas`:

```sql
BloqueaHorario AS CONVERT(BIT,
    CASE WHEN Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion')
         THEN 1 ELSE 0 END) PERSISTED
```

**Por qué `PERSISTED`.** Sin esa palabra, SQL Server recalcula la expresión en cada lectura y no permite indexarla. Con ella, el valor se guarda en disco y la columna se puede usar en `JOIN` sin penalización.

**Alternativa descartada: repetir la lista de estados en cada consulta.** Funciona hasta que se agrega un estado nuevo y alguien olvida actualizar uno de los cinco lugares. Ese tipo de defecto no rompe nada visiblemente: solo hace que una cita ocupe o libere un cupo cuando no debía.

**Limitación del motor que tocó documentar.** SQL Server **no permite** usar una columna calculada dentro del `WHERE` de un índice filtrado. Por eso `UX_Citas_Medico_HorarioActivo` repite la lista de estados literalmente en su filtro. Es una restricción del motor, no un descuido, y el script lo deja comentado para que nadie lo "arregle".

---

## ADR-06 — Índices únicos filtrados para las reglas de unicidad condicional

**Estado:** aceptada

**Contexto.** Dos reglas de unicidad no aplican a todas las filas: "no dos citas activas del mismo médico a la misma hora" solo aplica a las activas, y "no dos pacientes con el mismo DPI" solo aplica a los que tienen DPI.

**Decisión.** Índices únicos con `WHERE`.

```sql
CREATE UNIQUE INDEX UX_Citas_Medico_HorarioActivo
ON dbo.Citas (MedicoId, FechaHoraInicio, FechaHoraFin)
WHERE Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion');

CREATE UNIQUE INDEX UQ_Pacientes_Documento
ON dbo.Pacientes (Documento)
WHERE Documento IS NOT NULL AND Documento <> N'';
```

**Por qué el filtro es esencial en el primero.** Sin él, una cita cancelada bloquearía su cupo para siempre. Con él, al pasar a `Cancelada` la fila sale del índice y el horario se libera solo.

**Por qué el filtro es esencial en el segundo.** Los pacientes registrados en mostrador pueden no traer DPI. Un índice único sin filtro sobre una columna que admite nulos restringiría cuántos nulos pueden existir.

**Consecuencias.** El error que devuelve el motor es el 2601 o el 2627, que no son legibles. `SqlServerExceptionMapper` los traduce a mensajes en español antes de que lleguen al cliente, distinguiendo por el nombre del índice en el texto del error.

---

## ADR-07 — Eliminar las especialidades del modelo

**Estado:** aceptada, implementada

**Contexto.** El diseño original tenía `Especialidades`, `MedicoEspecialidad` y una columna obligatoria `Citas.EspecialidadId`. El flujo era: el paciente elige especialidad, el sistema le muestra los médicos de esa especialidad, y elige uno.

**El problema.** En la fase 1 la clínica tiene pocos médicos y cada especialidad terminaba con uno solo. El paso de elegir especialidad no aportaba información: el paciente elegía una especialidad y el sistema le ofrecía un único médico. Además la restricción de "un solo médico primario activo por especialidad" bloqueaba casos reales de cobertura entre colegas.

**Decisión.** Eliminar ambas tablas y la columna. El endpoint `/api/citas/disponibilidad` pasó a recibir solo `fecha` y devolver los cupos de **todos** los médicos activos.

**Alcance del cambio.** 59 archivos en 8 proyectos: 13 eliminados y el resto editados. La estrategia fue empezar por `Domain` y dejar que el compilador señalara cada punto afectado.

**Lo que obligó a tocar en la base.** El trigger `TR_Citas_Validaciones` tuvo que reescribirse: el error 51001 pasó de *"el médico y la especialidad deben estar activos y asociados"* a *"el médico no existe o no está activo"*. Ese cambio está en `database/ClinicaPro_CitasIntegridad.sql`.

**Consecuencias.** El flujo del paciente se simplificó a elegir fecha, hora y médico. Si en el futuro la clínica crece y necesita filtrar por especialidad, habrá que reintroducir el concepto, pero el modelo lo admite sin cambios estructurales: sería una tabla nueva y un filtro en la consulta de disponibilidad.

**Deuda que dejó.** Quedó residuo en los contratos: `EspecialidadDto.cs` sigue existiendo, y `CitaDto` y `AdminMedicoDto` mantienen campos anulables de especialidad que ya nunca se llenan. Se dejaron para no romper el frontend durante el refactor. Hoy son código muerto y se pueden eliminar.

---

## ADR-08 — La cola de autorización sin tabla propia

**Estado:** aceptada

**Contexto.** La tercera reprogramación de una cita requiere autorización de un administrador. Eso implica una cola: una secretaria solicita, un administrador aprueba o rechaza.

**Decisión.** La cola no tiene tabla propia. Se guarda como filas de `HistorialCitas` con `TipoCambio = 'Autorizacion'`, y la decisión final queda en `Citas.AutorizacionTerceraPorUsuarioId`.

**Por qué.** La base está alojada en un servicio compartido y agregar objetos nuevos requería coordinación. La información que la cola necesita —quién solicitó, quién decidió, cuándo, con qué motivo— ya cabe en las columnas de `HistorialCitas`.

**Cómo se implementó.** `AutorizacionReprogramacionRepository` reconstruye los objetos leyendo esas filas. En el `DbContext`, la entidad está explícitamente excluida del modelo:

```csharp
modelBuilder.Ignore<AutorizacionReprogramacion>();
```

Esa línea es la que evita que EF Core busque una tabla que no existe.

**Consecuencias.** A favor: cero cambios de esquema y la autorización queda en la misma bitácora que el resto de la historia de la cita. En contra: no se puede imponer con un índice único que haya una sola solicitud pendiente por cita, cosa que una tabla propia sí permitiría. El script `ClinicaPro_AutorizacionReprogramacion.sql` existe en el repositorio con esa tabla preparada, pero está marcado como opcional y no se aplicó.

---

## ADR-09 — La fecha de la cita en hora local, no en UTC

**Estado:** aceptada

**Contexto.** La convención general del sistema es guardar fechas en UTC. `Citas.FechaHoraInicio` y `FechaHoraFin` son la excepción: están en hora local de Guatemala.

**Decisión.** Mantener esa excepción, y documentarla.

**Por qué.** Una cita es un compromiso con una **hora de pared**. "El lunes a las nueve" debe seguir siendo a las nueve pase lo que pase. Guardarla en hora local tiene tres ventajas concretas:

1. Se compara directamente contra las columnas `TIME(0)` de `Horarios`, sin conversión en cada consulta.
2. El trigger puede verificar "no en el pasado" con una sola expresión.
3. Lo que se guarda es lo que el usuario escribió, así que ninguna conversión mal hecha puede correr la cita seis horas.

**Qué sí va en UTC.** Todos los sellos de tiempo de auditoría: `CreatedAtUtc`, `UpdatedAtUtc`, `FechaCambioUtc`, `FechaRegistroUtc`, `FechaCapturaUtc`. Esos marcan *cuándo ocurrió un evento en el sistema*, no *cuándo va a ocurrir algo en el mundo*.

**Dónde vive la conversión.** En un solo archivo, `src/ClinicaPro.Domain/HoraClinica.cs`. Resuelve la zona probando primero `Central America Standard Time` (Windows) y luego `America/Guatemala` (Linux), y si ninguna existe construye una zona fija en UTC−6. Esa doble búsqueda es necesaria porque el API corre en Windows durante el desarrollo y en Linux al desplegar.

**Consecuencias.** Es la fuente de confusión más frecuente al consumir la API: enviar `2026-10-27T09:00:00Z` corre la cita seis horas. Está documentado en la descripción de Swagger y en `API_ENDPOINTS.md`.

---

## ADR-10 — Las imágenes de iris cuelgan de la cita, no del paciente

**Estado:** aceptada

**Contexto.** Una imagen de iris pertenece conceptualmente a un paciente. La pregunta es con qué tabla se relaciona.

**Decisión.** `ImagenesIris` tiene `CitaId` y **no tiene `PacienteId`**.

**Por qué.** Una imagen clínica es evidencia de un acto médico concreto: qué día se tomó, qué médico atendía, en qué contexto. La cita identifica ese acto; el paciente solo. Al colgar de la cita, cada imagen conserva su contexto completo sin columnas adicionales.

**Lo que se gana.** No hay forma de que `PacienteId` quede inconsistente con el paciente de la cita, porque no existe. El paciente se alcanza con un `JOIN`.

**Lo que se pierde.** Listar todas las imágenes de un paciente cuesta un `JOIN` más. Es irrelevante al volumen de este sistema.

**Decisiones relacionadas.**

*El hash se calcula en el servidor.* En `ImagenIrisServices.cs`, con `Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant()`. Si lo enviara el cliente no verificaría nada, porque un cliente que altere la imagen también puede alterar el hash.

*El binario nunca se proyecta en los listados.* `ImagenIrisRepository.ListarPorCitaAsync` devuelve un `record` de metadatos sin la columna `Imagen`. Traer el binario para mostrar una lista de nombres de archivo movería decenas de megabytes sin necesidad. Hay una prueba, `ExpedientePaciente_MedicoConCita_ListaSignosSinBinario`, que protege esa decisión.

*El tamaño lo calcula la base.* `TamanoBytes` es una columna calculada, `DATALENGTH(Imagen)`. La aplicación no puede escribirla aunque quisiera.

---

## ADR-11 — La preconsulta se registra con `PUT` y es idempotente

**Estado:** aceptada

**Contexto.** Los signos vitales se toman en recepción. A veces hay que corregirlos: la secretaria se equivocó al teclear, o se repite la toma porque la primera salió rara.

**Decisión.** `PUT /api/citas/{citaId}/preconsulta`, con lógica de crear-o-actualizar. El servicio busca si ya existe una preconsulta activa para esa cita; si existe la actualiza, si no la crea.

**Por qué `PUT` y no `POST`.** Porque la operación es idempotente: llamarla dos veces con los mismos datos deja el sistema en el mismo estado. `POST` habría implicado una colección de preconsultas por cita y la pregunta de cuál es la vigente.

**Alternativa descartada: permitir varias preconsultas por cita, con fecha.** Daría un registro histórico de cada toma. Se descartó porque no hay requisito clínico de conservar las tomas descartadas, y porque complica la lectura: todo consumidor tendría que ordenar y elegir la última.

**Deuda conocida.** La regla de "una preconsulta activa por cita" la garantiza el servicio, **no la base**. Con dos peticiones simultáneas podrían entrar dos filas. La corrección propuesta es un índice único filtrado:

```sql
CREATE UNIQUE INDEX UX_Preconsultas_CitaActiva
ON dbo.Preconsultas (CitaId)
WHERE IsActive = 1;
```

Es la única regla importante del sistema que no tiene respaldo en la capa de datos, y está así por una decisión consciente de no pedir objetos nuevos en la base compartida.

---

## ADR-12 — La captura clínica solo en sala

**Estado:** aceptada, agregada en el PR #39

**Contexto.** Originalmente la preconsulta y las imágenes de iris se podían registrar sobre una cita en cualquier estado.

**Decisión.** Solo se aceptan con la cita en `En Espera` o `En Atencion`, verificado con `CitaEstados.PermiteCapturaClinica(estado)`.

**Por qué.** Porque el dato tiene que corresponder a la realidad física. Los signos vitales se toman cuando el paciente está en la clínica: ni antes de que llegue, ni después de que se fue. Registrar una presión arterial sobre una cita `Programada` para la semana siguiente es un dato inventado.

**Cómo se implementó.** Un único método en `CitaEstados` consultado por los dos servicios, en lugar de repetir la condición. Los mensajes de error nombran los dos estados válidos, para que el usuario sepa qué hacer.

**Consecuencias.** En el guion de pruebas manuales, el paso de registrar preconsulta tuvo que moverse después del paso de `llegada`. Intentarlo antes devuelve `400`, y eso es correcto.

---

## ADR-13 — Notificaciones encoladas, no sincrónicas

**Estado:** aceptada

**Contexto.** Cada cambio de estado de una cita debe notificar al paciente por correo.

**Decisión.** El envío no es sincrónico. El servicio encola una fila en `Notificaciones` con estado `Pendiente`, y el servicio en segundo plano `NotificationDispatchWorker` la toma y la intenta enviar, registrando cada intento en `IntentosNotificacion`.

**Por qué.** Porque una caída del servidor de correo no debe hacer fallar la creación de una cita. Si el envío fuera sincrónico, un SMTP lento convertiría una operación de 200 milisegundos en una de treinta segundos, y uno caído la haría fallar por completo.

**Consecuencias.** El correo no es inmediato: llega en el siguiente ciclo del worker. A cambio, los fallos de envío quedan registrados con la respuesta del proveedor y se pueden diagnosticar sin revisar los registros del servidor.

**Modo de desarrollo.** Sin `Smtp:Host` configurado, el worker escribe los correos como archivos en `App_Data/mail` en lugar de enviarlos. Permite verificar el contenido sin una cuenta real.

**Estado actual.** No hay credenciales SMTP configuradas en el repositorio, así que todas las notificaciones quedan en `Fallida`. El mecanismo está completo y se puede demostrar con los archivos de `App_Data/mail`. Falta definir la cuenta de correo del proyecto, y reemplazar el remitente por omisión `noreply@clinica.local`, que es un dominio inexistente.

---

## ADR-14 — `Contracts` como proyecto separado

**Estado:** aceptada

**Contexto.** El backend y el cliente Blazor WebAssembly necesitan compartir los tipos de los mensajes HTTP.

**Decisión.** Un proyecto `ClinicaPro.Contracts` que contiene solo `record` de petición y respuesta, referenciado por `Api` y por `Client`.

**La restricción que impone.** `Contracts` **no puede referenciar ASP.NET Core**, porque lo consume un proyecto de WebAssembly que corre en el navegador.

**El caso concreto donde se notó.** Al implementar la subida de imágenes de iris, la clase del formulario necesitaba un `IFormFile`, que vive en `Microsoft.AspNetCore.Http`. Ponerla en `Contracts` produjo siete errores de compilación en el cliente. La solución fue dejarla en `src/ClinicaPro.Api/Controllers/SubirImagenIrisForm.cs`: es un detalle de transporte del servidor, no un contrato compartido.

**Consecuencias.** Cualquier cambio en `Contracts` es un cambio de contrato que afecta al frontend y debe avisarse. Ocurrió con la adición de `NombreCompleto` a `AuthResponse` en el PR #34.

---

## ADR-15 — Nombres de tablas en español, incluidas las de Identity

**Estado:** aceptada

**Contexto.** ASP.NET Core Identity trae nombres de tabla en inglés: `AspNetUsers`, `AspNetRoles`, `AspNetUserRoles`.

**Decisión.** Renombrarlas a `Usuarios`, `Roles`, `UsuarioRoles`, y usar `Guid` como tipo de llave en lugar del `string` por omisión.

**Por qué.** Consistencia: el resto del esquema está en español y mezclar convenciones en la misma base dificulta leerla. El `Guid` se eligió para que todas las llaves primarias del sistema sean del mismo tipo.

**Costo.** Obliga a mapear Identity explícitamente en `IdentityTablesConfiguration.cs`, y a declarar las siete clases de `Infrastructure/Identity/`. También obliga a conservar las cuatro tablas que Identity exige aunque no se usen —`RolClaims`, `UsuarioClaims`, `UsuarioLogins`, `UsuarioTokens`—, porque `IdentityDbContext` las declara en su modelo y sin ellas la aplicación no arranca.

**Consecuencias.** Un desarrollador que conozca Identity por su documentación no reconocerá los nombres de inmediato. Es un costo aceptado a cambio de un esquema coherente.

---

## ADR-16 — Concurrencia optimista con `ROWVERSION`

**Estado:** aceptada

**Contexto.** Dos personas pueden actuar sobre la misma cita al mismo tiempo: la secretaria la confirma mientras el paciente la anula desde su portal.

**Decisión.** Concurrencia optimista con una columna `ROWVERSION` en `Citas`, `Preconsultas` e `ImagenesIris`, mapeada con `.IsRowVersion()`.

**Cómo funciona.** SQL Server incrementa el contador automáticamente en cada cambio. EF Core guarda el valor al leer y lo incluye en el `WHERE` del `UPDATE`. Si otro proceso modificó la fila, el valor ya no coincide, el `UPDATE` afecta cero filas y EF lanza `DbUpdateConcurrencyException`.

**Por qué optimista y no pesimista.** La alternativa —bloquear la fila al leerla— haría que una secretaria con una pantalla abierta bloqueara la cita para todos los demás hasta cerrarla. El conflicto real es raro; detectarlo después sale más barato que prevenirlo siempre.

**Consecuencias.** Sin `RowVersion`, el último en guardar gana en silencio y uno de los dos nunca se entera de que su cambio se perdió. Con él, el segundo recibe un error y debe recargar.

---

## ADR-17 — `MultipleActiveResultSets=True` en la cadena de conexión

**Estado:** aceptada, con una consecuencia que conviene conocer

**Contexto.** Varias operaciones necesitan tener más de un lector abierto sobre la misma conexión.

**Decisión.** Activar MARS en la cadena de conexión.

**La consecuencia no obvia.** MARS **desactiva los puntos de guardado (`savepoints`) de EF Core**. En la práctica significa que una transacción no se puede revertir parcialmente: falla completa o pasa completa.

**Por qué se acepta.** Ninguna operación del sistema necesita revertir parcialmente. Todas son atómicas por naturaleza: una cita se crea o no se crea.

**Por qué se documenta igual.** Porque si en el futuro alguien implementa una operación por lotes que deba continuar tras un fallo parcial, descubrirá que no puede, y el motivo no es evidente al leer el código.

---

## ADR-18 — Qué se puede cambiar sin recompilar

**Estado:** aceptada

**Contexto.** Algunas reglas de negocio son más volátiles que otras. "La duración de una cita son 30 minutos" puede cambiar; "una cita atendida no vuelve a estar en espera" no.

**Decisión.** Separar las reglas en tres niveles según dónde viven y qué cuesta cambiarlas.

| Nivel | Qué contiene | Cómo se cambia |
|---|---|---|
| Parámetros | `Citas.HorasMinimasCancelacion`, `Citas.DuracionPredeterminadaMinutos` | `PUT /api/admin/parametros/{clave}`, en caliente, solo Administrador |
| Base de datos | Triggers, `CHECK`, índices, columnas calculadas | Script SQL y despliegue coordinado |
| Código | Transiciones de estado y permisos por rol, en `Cita.cs` y los atributos `[Authorize]` | Recompilar y desplegar |

**El caso intermedio.** `Citas.MaximoReprogramaciones` está en la tabla de parámetros pero **deliberadamente bloqueado para edición**: el tope de 3 es una regla fija del documento de arquitectura, replicada en el `CHECK CK_Citas_Reprogramaciones`. Cambiarlo por la pantalla dejaría el parámetro y la base en desacuerdo.

**Deuda conocida.** `Citas.MaximoActivasPorPaciente` está declarada en `ParametrosClave` y la consulta `ValidarAgendaPacienteService`, pero **no está sembrada en la tabla**. El servicio cae al valor por defecto del dominio, que es 3. La regla funciona, pero el administrador no puede ajustarla. Se corrige con una sola inserción.

---

## Anexo — Divergencias entre el documento de arquitectura y la implementación

Diferencias detectadas al cruzar el documento de arquitectura con el código. Ninguna es un defecto funcional; todas requieren una decisión del equipo sobre si se ajusta el documento o el código.

| # | El documento dice | El código hace | Propuesta |
|---|---|---|---|
| 1 | La revalidación al confirmar ocurre en la capa de aplicación, dentro de una transacción explícita | La hace el trigger durante el `UPDATE`, en la transacción implícita de la sentencia | El efecto es equivalente. Ajustar el documento para describir lo implementado |
| 2 | La ventana de cancelación sale del parámetro | La sobrecarga `Cita.Cancelar()` sin argumentos tiene el `2` escrito en el código | Eliminar esa sobrecarga: las demás sí leen el parámetro |
| 3 | — | El error 51006 se usa para dos reglas distintas: solape de paciente y falta de `SESSION_CONTEXT` | Reasignar una de las dos a 51007 |
| 4 | Una especialidad tiene un único médico primario activo | Las especialidades se eliminaron del modelo (ADR-07) | Actualizar el documento |
| 5 | — | `/api/citas/disponibilidad` es anónimo | Confirmar la decisión y documentar el motivo |
