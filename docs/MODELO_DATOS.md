# Modelo de datos — Clínica Quantium

Documentación del esquema de la base de datos del módulo de gestión de citas.

**Motor:** SQL Server 2022 Express (alojado en Somee)
**Esquema:** `dbo`
**Tablas:** 18
**Elaborado por:** Allan Ricardo Quiej Ixmatá
**Rama de referencia:** `develop` @ `43d8791`

---

## 1. Convenciones del esquema

Estas reglas se aplican de forma uniforme y conviene conocerlas antes de leer cada tabla.

| Convención | Detalle | Por qué |
|---|---|---|
| Llaves primarias de entidad | `UNIQUEIDENTIFIER` con `DEFAULT NEWSEQUENTIALID()` | El GUID permite generar el identificador en la aplicación antes de insertar. `NEWSEQUENTIALID` lo genera casi secuencial para no fragmentar el índice agrupado |
| Llaves primarias de bitácora | `BIGINT IDENTITY(1,1)` | En tablas de solo escritura el orden de inserción es información útil, y un entero ocupa la mitad |
| Borrado | Ninguna llave foránea usa `ON DELETE CASCADE`: todas son `NO ACTION` | Un expediente clínico no se borra. La baja es lógica, con `IsActive = 0` |
| Marcas de tiempo | `DATETIME2(0)` en UTC, con `DEFAULT SYSUTCDATETIME()` | Precisión de un segundo, suficiente para auditoría, y sin ambigüedad de zona horaria |
| La excepción | `Citas.FechaHoraInicio` y `Citas.FechaHoraFin` están en **hora local de Guatemala** | Ver la sección 4 |
| Concurrencia | `ROWVERSION` en `Citas`, `Preconsultas` e `ImagenesIris` | Concurrencia optimista: ver la sección 7 |
| Nombres | Tablas y columnas en español, incluidas las de ASP.NET Core Identity | Decisión del equipo tomada al inicio. Obliga a mapear Identity explícitamente en `IdentityTablesConfiguration.cs` |

---

## 2. Las 18 tablas por grupo

### Seguridad (7 tablas)

| Tabla | Qué guarda |
|---|---|
| `Roles` | Los cuatro roles del sistema. Sembrados con GUID fijos `10000000-0000-0000-0000-00000000000X` |
| `Usuarios` | Identidad de ASP.NET Core con nombres en español. Además de las columnas estándar tiene `IsActive` y `MustChangePassword` |
| `UsuarioRoles` | Tabla puente usuario–rol. Llave primaria compuesta `(UsuarioId, RoleId)` |
| `RolClaims` | Requerida por `IdentityDbContext`. Sin uso en este proyecto |
| `UsuarioClaims` | Requerida por `IdentityDbContext`. Sin uso en este proyecto |
| `UsuarioLogins` | Requerida por `IdentityDbContext`. Sería para inicio de sesión externo (Google, Microsoft) |
| `UsuarioTokens` | Requerida por `IdentityDbContext`. Guarda tokens de recuperación de contraseña |

Las últimas cuatro están vacías pero **no pueden eliminarse**: `IdentityDbContext` las declara en su modelo y sin ellas la aplicación no arranca.

### Personas (2 tablas)

| Tabla | Qué guarda |
|---|---|
| `Pacientes` | Ficha del paciente. `UsuarioId` es único: un usuario tiene a lo sumo un perfil de paciente |
| `Medicos` | Ficha del médico, con `NumeroColegiado`. También uno a uno con `Usuarios` |

### Agenda (2 tablas)

| Tabla | Qué guarda |
|---|---|
| `Horarios` | Jornada semanal de cada médico: día, hora de inicio y fin, vigencia opcional |
| `Citas` | La tabla central del sistema. Ver la sección 4 |

### Clínico (2 tablas)

| Tabla | Qué guarda |
|---|---|
| `Preconsultas` | Signos vitales tomados en recepción antes de la consulta |
| `ImagenesIris` | Imágenes de iris capturadas durante la atención, almacenadas como binario |

### Trazabilidad (2 tablas)

| Tabla | Qué guarda |
|---|---|
| `HistorialCitas` | Bitácora automática de cada cambio de una cita. La escribe un trigger, nunca la aplicación |
| `Auditoria` | Bitácora de acciones administrativas. La escribe la aplicación |

### Notificaciones (2 tablas)

| Tabla | Qué guarda |
|---|---|
| `Notificaciones` | Cola de mensajes por enviar, con su estado y número de intentos |
| `IntentosNotificacion` | Un registro por cada intento de envío, con la respuesta del proveedor |

### Configuración (1 tabla)

| Tabla | Qué guarda |
|---|---|
| `Parametros` | Reglas de negocio editables en caliente. La llave primaria es la propia clave en texto |

---

## 3. Diagrama de relaciones

```
                       Usuarios
                      /    |    \
                     /     |     \
              Pacientes  Medicos  (CreadaPor, SecretariaResponsable,
                   |       |       AutorizacionTercera, UpdatedBy)
                   |       |
                   |    Horarios
                   |
                   +-------+
                           |
                         Citas  <-- la tabla central
                           |
         +-----------+-----+------+--------------+
         |           |            |              |
  HistorialCitas  Preconsultas  ImagenesIris  Notificaciones
                                                   |
                                          IntentosNotificacion

   Parametros y Auditoria no dependen de ninguna otra tabla.
```

Leído en palabras: un `Usuario` puede ser un `Paciente` **o** un `Medico`, nunca ambos a la vez en la práctica, porque `UsuarioId` es único en cada una de esas dos tablas. Un `Medico` tiene muchos `Horarios`. Una `Cita` apunta a un `Paciente` y a un `Medico`, y de ella cuelgan su historial, su preconsulta, sus imágenes de iris y sus notificaciones.

`Citas` tiene además tres llaves foráneas hacia `Usuarios` que no representan al paciente ni al médico: `CreadaPorUsuarioId` (quién la registró, que puede ser el paciente o una secretaria), `SecretariaResponsableId` (quién la confirmó o rechazó) y `AutorizacionTerceraPorUsuarioId` (qué administrador autorizó la tercera reprogramación).

---

## 4. `dbo.Citas` en detalle

Es la tabla que concentra las reglas de negocio del módulo.

### Columnas

| Columna | Tipo | Nulo | Nota |
|---|---|---|---|
| `CitaId` | `UNIQUEIDENTIFIER` | No | PK, `NEWSEQUENTIALID()` |
| `PacienteId` | `UNIQUEIDENTIFIER` | No | FK → `Pacientes` |
| `MedicoId` | `UNIQUEIDENTIFIER` | No | FK → `Medicos` |
| `FechaHoraInicio` | `DATETIME2(0)` | No | **Hora de Guatemala**, no UTC |
| `FechaHoraFin` | `DATETIME2(0)` | No | **Hora de Guatemala**, no UTC |
| `MotivoConsulta` | `NVARCHAR(500)` | No | Mínimo 5 caracteres útiles |
| `Estado` | `NVARCHAR(20)` | No | Uno de los nueve valores permitidos |
| `NumeroReprogramaciones` | `TINYINT` | No | Entre 0 y 3 |
| `AutorizacionTerceraPorUsuarioId` | `UNIQUEIDENTIFIER` | Sí | FK → `Usuarios`. Obligatoria si el contador llega a 3 |
| `CreadaPorUsuarioId` | `UNIQUEIDENTIFIER` | No | FK → `Usuarios` |
| `SecretariaResponsableId` | `UNIQUEIDENTIFIER` | Sí | FK → `Usuarios` |
| `CreatedAtUtc` | `DATETIME2(0)` | No | UTC |
| `UpdatedAtUtc` | `DATETIME2(0)` | Sí | UTC |
| `RowVersion` | `ROWVERSION` | No | Concurrencia optimista |
| `BloqueaHorario` | `BIT` calculada, `PERSISTED` | No | Derivada de `Estado` |

### Por qué la fecha de la cita no está en UTC

Es la decisión más contraintuitiva del esquema y hay que poder defenderla.

Una cita es un compromiso con una **hora de pared**: "el lunes a las nueve" tiene que seguir siendo a las nueve, sin importar qué pase con husos horarios o cambios de horario de verano. Guardarla en hora local de Guatemala tiene tres ventajas concretas:

1. Se compara directamente contra las columnas `TIME(0)` de `Horarios`, sin conversión.
2. El trigger de validación puede comprobar "no en el pasado" con una sola expresión.
3. Lo que se guarda es lo que el usuario escribió, así que no hay forma de que una conversión mal hecha corra la cita seis horas.

El resto de las columnas de fecha del sistema —`CreatedAtUtc`, `UpdatedAtUtc`, `FechaCambioUtc`— sí son sellos de tiempo de auditoría y van en UTC.

La conversión entre ambos mundos vive en un solo lugar: `src/ClinicaPro.Domain/HoraClinica.cs`.

### Restricciones `CHECK`

| Restricción | Regla |
|---|---|
| `CK_Citas_Rango` | `FechaHoraFin > FechaHoraInicio` |
| `CK_Citas_MismoDia` | La cita empieza y termina el mismo día calendario |
| `CK_Citas_Motivo` | `LEN(LTRIM(RTRIM(MotivoConsulta))) >= 5` |
| `CK_Citas_Estado` | `Estado` debe ser uno de los nueve valores |
| `CK_Citas_Reprogramaciones` | `NumeroReprogramaciones BETWEEN 0 AND 3` |
| `CK_Citas_TerceraReprogramacion` | `NumeroReprogramaciones < 3 OR AutorizacionTerceraPorUsuarioId IS NOT NULL` |

La última es un `CHECK` cruzado entre dos columnas, no sobre una sola. Garantiza que sea imposible que exista en la base una cita con tres reprogramaciones y sin autorización registrada, aunque alguien escriba por SQL directo.

### Índices

| Índice | Columnas | Tipo |
|---|---|---|
| `PK_Citas` | `CitaId` | Agrupado, único |
| `IX_Citas_MedicoId_Fecha` | `MedicoId, FechaHoraInicio` | Con `INCLUDE` de fin, estado y paciente |
| `IX_Citas_Estado` | `Estado, FechaHoraInicio` | Para la cola de pendientes |
| `IX_Citas_PacienteId_Fecha` | `PacienteId, FechaHoraInicio DESC` | Para "mis citas" |
| `UX_Citas_Medico_HorarioActivo` | `MedicoId, FechaHoraInicio, FechaHoraFin` | **Único y filtrado** |

---

## 5. Las dos columnas calculadas

### `Citas.BloqueaHorario`

```sql
BloqueaHorario AS CONVERT(BIT,
    CASE WHEN Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion')
         THEN 1 ELSE 0 END) PERSISTED
```

Una columna calculada se define con una fórmula sobre otras columnas de la misma fila. `PERSISTED` indica que SQL Server guarda el resultado en disco en lugar de recalcularlo en cada lectura, lo cual permite indexarla y usarla en condiciones de `JOIN` sin penalización.

Responde a una sola pregunta: *¿esta cita está ocupando un espacio en la agenda?* Esa pregunta aparece en el trigger de validación, en el cálculo de disponibilidad y en varias consultas de reporte. Centralizarla evita que la lista de cinco estados quede desactualizada en alguno de esos lugares.

**Limitación del motor que vale la pena documentar:** SQL Server no permite usar una columna calculada dentro del `WHERE` de un índice filtrado. Por eso `UX_Citas_Medico_HorarioActivo` repite la lista de estados literalmente en su filtro. No es un descuido, es una restricción de SQL Server, y el script original lo deja comentado.

### `ImagenesIris.TamanoBytes`

Se calcula como `DATALENGTH(Imagen)`. La aplicación nunca la escribe.

En Entity Framework Core hubo que mapearla explícitamente:

```csharp
builder.Property(imagen => imagen.TamanoBytes)
    .HasComputedColumnSql()
    .ValueGeneratedOnAddOrUpdate();
```

Sin esas dos llamadas, EF Core incluye la columna en el `INSERT` y SQL Server responde con el error 271: *"The column TamanoBytes cannot be modified because it is either a computed column or is the result of a UNION operator."*

En la entidad de dominio, la propiedad tiene `private set` y **nunca se asigna en ningún constructor ni método**. Es deliberado: el valor viene de la base.

---

## 6. Los dos índices únicos filtrados

Un índice filtrado solo indexa las filas que cumplen su `WHERE`. Al ser único, obliga a que la combinación de columnas no se repita **entre esas filas**, dejando libres las demás.

### `UX_Citas_Medico_HorarioActivo`

```sql
CREATE UNIQUE INDEX UX_Citas_Medico_HorarioActivo
ON dbo.Citas (MedicoId, FechaHoraInicio, FechaHoraFin)
WHERE Estado IN (N'Solicitada', N'Programada', N'Confirmada', N'En Espera', N'En Atencion');
```

Impide dos citas idénticas del mismo médico a la misma hora, pero solo entre las que bloquean agenda. Si el índice fuera único sin filtro, una cita cancelada bloquearía ese cupo para siempre; con el filtro, al pasar a `Cancelada` la fila sale del índice y el horario se libera automáticamente.

Esta es la garantía más fuerte contra reservas duplicadas, porque la aplica el motor y no depende de que el código consulte antes de insertar.

### `UQ_Pacientes_Documento`

```sql
CREATE UNIQUE INDEX UQ_Pacientes_Documento
ON dbo.Pacientes (Documento)
WHERE Documento IS NOT NULL AND Documento <> N'';
```

Permite que varios pacientes registrados en mostrador no tengan DPI, pero impide que un DPI se repita. Sin el filtro, un índice único sobre una columna que admite nulos permitiría un solo `NULL` en toda la tabla en algunos motores, y aquí se necesitan muchos.

---

## 7. `ROWVERSION` y concurrencia optimista

`Citas`, `Preconsultas` e `ImagenesIris` tienen una columna `RowVersion` de tipo `ROWVERSION`, mapeada en EF Core con `.IsRowVersion()`.

`ROWVERSION` es un contador binario de 8 bytes que SQL Server incrementa automáticamente cada vez que la fila cambia. No es una fecha y no se puede escribir.

EF Core lo usa así: al leer una entidad guarda el valor, y al actualizarla lo incluye en el `WHERE` del `UPDATE`. Si otro proceso modificó la fila mientras tanto, el valor ya no coincide, el `UPDATE` afecta cero filas y EF lanza `DbUpdateConcurrencyException`.

Se llama **optimista** porque no bloquea nada: asume que el conflicto es raro y lo detecta después, en lugar de poner un candado antes. La alternativa pesimista —bloquear la fila al leerla— haría que una secretaria con una pantalla abierta bloqueara la cita para todos los demás.

El escenario concreto que protege: la secretaria abre una cita para confirmarla mientras el paciente la anula desde su portal. Sin `RowVersion`, el último en guardar gana en silencio y uno de los dos nunca se entera.

---

## 8. Los triggers

### `TR_Citas_Validaciones`

Se dispara `AFTER INSERT, UPDATE` sobre `dbo.Citas` y valida seis reglas. Cada una lanza un error numerado.

| Error | Regla |
|---|---|
| 51000 | No se permite crear ni reprogramar una cita en fecha u hora pasada. Compara contra la hora de Guatemala |
| 51001 | El médico debe existir y estar activo |
| 51002 | La cita debe caer dentro del horario laboral configurado para ese médico, ese día de la semana, dentro de la vigencia |
| 51003 | No puede haber otra cita activa del mismo **médico** que se solape en el tiempo |
| 51004 | La tercera reprogramación exige que `AutorizacionTerceraPorUsuarioId` corresponda a un usuario con rol Administrador activo |
| 51005 | La transición de estado debe estar permitida por la máquina de estados |
| 51006 | No puede haber otra cita activa del mismo **paciente** que se solape en el tiempo |

La máquina de estados que valida el 51005:

```
Solicitada   ->  Programada, Rechazada
Programada   ->  Confirmada, En Espera, Cancelada, No presentada
Confirmada   ->  En Espera, Cancelada, No presentada
En Espera    ->  En Atencion, Atendida, No presentada
En Atencion  ->  Atendida
```

Los estados `Atendida`, `Cancelada`, `No presentada` y `Rechazada` son finales: no tienen transición de salida.

### `TR_Citas_Historial`

Se dispara `AFTER INSERT, UPDATE` sobre `dbo.Citas` y escribe en `HistorialCitas`. Clasifica el cambio en cuatro tipos: `Creacion`, `CambioEstado`, `Reprogramacion` y `Cancelacion`. Un quinto tipo, `Autorizacion`, lo escribe la aplicación.

Para saber **quién** hizo el cambio usa `SESSION_CONTEXT`, explicado en la sección siguiente. Si un cambio de estado o de fecha llega sin contexto, lanza el error **51006** y aborta la operación completa.

> **Colisión de numeración conocida.** El 51006 se usa para dos reglas distintas: solape de paciente en `TR_Citas_Validaciones` y falta de `SESSION_CONTEXT` en `TR_Citas_Historial`. Funciona porque los mensajes difieren, pero cualquier código que decida por número no puede distinguirlas. La corrección propuesta es reasignar una de las dos a 51007.

---

## 9. `SESSION_CONTEXT`: cómo el trigger sabe quién actuó

**El problema.** Un trigger solo ve las tablas virtuales `inserted` y `deleted`. Ninguna de las dos trae el usuario de la aplicación, porque todas las conexiones entran a SQL Server con la misma credencial del sistema.

**La solución.** La aplicación deja el dato en la sesión de la conexión, inmediatamente antes del `UPDATE` y dentro de la misma transacción:

```sql
EXEC sys.sp_set_session_context @key = N'UsuarioId',    @value = '<guid del usuario>';
EXEC sys.sp_set_session_context @key = N'MotivoCambio', @value = N'Confirmacion administrativa';

UPDATE dbo.Citas SET Estado = N'Programada' WHERE CitaId = ...;
```

El trigger lo recoge:

```sql
DECLARE @UsuarioContexto UNIQUEIDENTIFIER =
    TRY_CONVERT(UNIQUEIDENTIFIER, SESSION_CONTEXT(N'UsuarioId'));
```

Quien lo hace del lado de la aplicación es `EfUnitOfWork.SaveChangesWithSqlSessionContextAsync`, en `src/ClinicaPro.Infrastructure/Persistence/EfUnitOfWork.cs`, que envuelve las tres sentencias en una transacción y las ejecuta dentro de un `ExecutionStrategy` para soportar los reintentos de conexión.

**La consecuencia importante del diseño:** como el trigger aborta si no hay contexto, es imposible cambiar el estado de una cita sin dejar rastro. Ni desde el API, ni desde DBeaver, ni desde una consulta manual. El historial no se puede evadir.

**La trampa que hay que conocer:** el contexto vive en la conexión, no en la transacción. Con un *pool* de conexiones mal manejado, un valor podría filtrarse a otra petición. Aquí no ocurre porque se escribe inmediatamente antes de cada `SaveChanges`, siempre con el valor correcto de esa operación.

---

## 10. Las tablas clínicas

### `dbo.Preconsultas`

| Columna | Tipo | Nota |
|---|---|---|
| `PreconsultaId` | `UNIQUEIDENTIFIER` | PK |
| `CitaId` | `UNIQUEIDENTIFIER` | FK → `Citas` |
| `PresionSistolicaMmHg` | `SMALLINT` | Validada entre 60 y 250 en el dominio |
| `PresionDiastolicaMmHg` | `SMALLINT` | Validada entre 40 y 150, y siempre menor que la sistólica |
| `TemperaturaCelsius` | `DECIMAL(4,1)` | Validada entre 30 y 45 |
| `OxigenoSangrePorcentaje` | `DECIMAL(5,2)` | Validada entre 50 y 100 |
| `Observacion` | `NVARCHAR(1000)` | Opcional |
| `RegistradaPorUsuarioId` | `UNIQUEIDENTIFIER` | FK → `Usuarios` |
| `FechaRegistroUtc` | `DATETIME2(0)` | UTC |
| `IsActive` | `BIT` | Baja lógica |
| `CreatedAtUtc`, `UpdatedAtUtc` | `DATETIME2(0)` | UTC |
| `RowVersion` | `ROWVERSION` | Concurrencia |

Los tipos decimales fueron elegidos por precisión mínima suficiente: `DECIMAL(4,1)` da hasta 999.9, de sobra para una temperatura con un decimal; `DECIMAL(5,2)` da hasta 999.99, suficiente para un porcentaje con dos decimales.

**Regla sin respaldo en la base.** El sistema garantiza una sola preconsulta activa por cita, pero esa regla vive únicamente en `PreconsultaServices.cs`, que busca la existente y la actualiza en vez de insertar otra. Con dos peticiones simultáneas podrían entrar dos filas. La corrección propuesta es un índice único filtrado:

```sql
CREATE UNIQUE INDEX UX_Preconsultas_CitaActiva
ON dbo.Preconsultas (CitaId)
WHERE IsActive = 1;
```

### `dbo.ImagenesIris`

| Columna | Tipo | Nota |
|---|---|---|
| `ImagenIrisId` | `UNIQUEIDENTIFIER` | PK |
| `CitaId` | `UNIQUEIDENTIFIER` | FK → `Citas` |
| `TomadaPorUsuarioId` | `UNIQUEIDENTIFIER` | FK → `Usuarios` |
| `Lateralidad` | `NVARCHAR(20)` | `OD`, `OI`, `Ambos` o `No especificada` |
| `NombreArchivo` | `NVARCHAR(255)` | Nombre original del archivo subido |
| `TipoContenido` | `NVARCHAR(100)` | `image/jpeg`, `image/png` o `image/webp` |
| `Imagen` | `VARBINARY(MAX)` | El binario completo |
| `TamanoBytes` | `BIGINT` **calculada** | `DATALENGTH(Imagen)` |
| `HashSha256` | `CHAR(64)` | Calculado en el servidor |
| `Observacion` | `NVARCHAR(500)` | Opcional |
| `FechaCapturaUtc` | `DATETIME2(0)` | UTC |
| `IsActive` | `BIT` | Baja lógica |
| `RowVersion` | `ROWVERSION` | Concurrencia |

**Tres decisiones de diseño que conviene documentar:**

*No hay columna `PacienteId`.* La imagen cuelga de la cita, no del paciente, porque una imagen clínica es evidencia de un acto médico concreto: qué día, con qué médico, en qué contexto. El paciente se alcanza por la cita, así que no se pierde información, y se evita una columna redundante que podría quedar inconsistente.

*El hash se calcula en el servidor.* En `ImagenIrisServices.cs`, con `Convert.ToHexString(SHA256.HashData(contenido)).ToLowerInvariant()`. Si lo mandara el cliente no verificaría nada, porque un cliente que altere la imagen también puede alterar el hash. Sirve para dos cosas: detectar corrupción del binario y encontrar duplicados exactos sin comparar megabytes.

*El binario nunca se proyecta en los listados.* `ImagenIrisRepository.ListarPorCitaAsync` devuelve un `record` de metadatos que no incluye `Imagen`. Traer el binario en una lista de diez imágenes significaría mover decenas de megabytes para mostrar nombres de archivo. Para descargar el contenido existe un endpoint aparte.

---

## 11. Las otras tablas

### `dbo.HistorialCitas`

Bitácora de solo inserción. Llave `BIGINT IDENTITY`. Guarda el estado y la fecha antes y después de cada cambio, el usuario que lo hizo y el motivo.

El campo `TipoCambio` acepta cinco valores: `Creacion`, `CambioEstado`, `Reprogramacion`, `Cancelacion` y `Autorizacion`.

**Detalle de implementación importante:** las consultas deben ordenar por `HistorialCitaId`, no por `FechaCambioUtc`. Varios registros escritos en la misma transacción comparten el mismo valor de `SYSUTCDATETIME()` al segundo, así que ordenar por fecha devuelve un orden arbitrario entre ellos. El `IDENTITY` sí es estrictamente creciente.

### `dbo.Auditoria`

Bitácora de la aplicación, no de la base. La escribe `AuditoriaWriter` desde la capa de infraestructura. Registra acción, entidad, identificador de entidad, detalle libre, dirección IP y `CorrelationId`.

Se diferencia de `HistorialCitas` en tres cosas: es general y no específica de citas, la escribe el código y no un trigger, y es opcional —si falla no revierte la operación.

### `dbo.Notificaciones` e `IntentosNotificacion`

El envío de correos no es sincrónico. Cuando una cita cambia de estado, el servicio encola una fila en `Notificaciones` con estado `Pendiente`, y el servicio en segundo plano `NotificationDispatchWorker` la toma y la intenta enviar.

| Estado | Significado |
|---|---|
| `Pendiente` | En cola, esperando el siguiente ciclo del worker |
| `Procesando` | El worker la tomó y está intentando enviarla |
| `Enviada` | El proveedor la aceptó |
| `Fallida` | Se agotaron los intentos |

Cada intento queda registrado en `IntentosNotificacion` con la respuesta del proveedor, lo cual permite diagnosticar por qué falló un envío sin revisar los registros del servidor.

El beneficio de este diseño es que una caída del servidor de correo no hace fallar la creación de la cita. El costo es que el envío no es inmediato.

### `dbo.Parametros`

Reglas de negocio editables sin recompilar. La llave primaria es la clave en texto, y `TipoDato` indica cómo interpretar el valor.

| Clave sembrada | Valor | Para qué sirve |
|---|---|---|
| `Citas.HorasMinimasCancelacion` | `2` | Cancelar con menos anticipación marca la cita como `No presentada` en lugar de `Cancelada` |
| `Citas.MaximoReprogramaciones` | `3` | Tope de reprogramaciones. Deliberadamente bloqueado para edición |
| `Citas.DuracionPredeterminadaMinutos` | `30` | Largo de cada cupo de la agenda |

**Hallazgo abierto:** el código declara una cuarta clave, `Citas.MaximoActivasPorPaciente`, en `ParametrosClave`, y `ValidarAgendaPacienteService` la consulta. Pero **no está sembrada en la tabla**, así que el servicio cae al valor por defecto del dominio, que es 3. La regla funciona, pero el administrador no puede cambiarla desde la pantalla de parámetros. Se corrige con una sola inserción.

---

## 12. Lo que fue eliminado del modelo

El diseño original incluía dos tablas más y una columna que ya no existen:

| Elemento | Qué era |
|---|---|
| `Especialidades` | Catálogo de especialidades médicas, con cinco filas sembradas |
| `MedicoEspecialidad` | Tabla puente con una bandera `EsPrimario` y un índice único filtrado que garantizaba un solo médico primario activo por especialidad |
| `Citas.EspecialidadId` | Columna obligatoria con FK a `Especialidades` |

El equipo decidió eliminarlas: el paciente elige fecha, hora y médico entre los cupos disponibles, no una especialidad. El cambio tocó 59 archivos en 8 proyectos y obligó a reescribir el trigger de validación, porque el error 51001 pasó de *"el médico y la especialidad deben estar activos y asociados"* a simplemente *"el médico no existe o no está activo"*.

Queda residuo en los contratos que conviene limpiar: `EspecialidadDto.cs` sigue existiendo con registros vivos, y `CitaDto` y `AdminMedicoDto` mantienen campos anulables de especialidad que ya nunca se llenan. Se dejaron durante el refactor para no romper el frontend y hoy son código muerto.

---

## 13. Scripts de base de datos

Los scripts viven en la carpeta `database/` y se ejecutaron en este orden. **El primero no debe volver a ejecutarse sobre la base viva.**

| Orden | Archivo | Qué hace |
|---|---|---|
| 1 | `ClinicaPro_BD_Somee.sql` | Esquema inicial completo: tablas, índices, triggers y semillas. Todavía contiene `Especialidades` |
| 2 | `ClinicaPro_PacienteMostrador.sql` | Agrega `Sexo`, `Alergias` y contacto de emergencia a `Pacientes` |
| 3 | `ClinicaPro_DpiUnico.sql` | Crea el índice único filtrado sobre `Documento` |
| 4 | `ClinicaPro_LlegadaProgramada.sql` | Reescribe el trigger para permitir `Programada` → `En Espera` |
| 5 | `ClinicaPro_CitasIntegridad.sql` | **El vigente.** Reescribe el trigger sin `Especialidades` y agrega la regla de solape del paciente |
| — | `ClinicaPro_AutorizacionReprogramacion.sql` | Opcional, no aplicado. El API resuelve la cola de autorización leyendo `HistorialCitas` |
| — | `ClinicaPro_Validacion_Somee.sql` | Validaciones estructurales posteriores a la carga |

Las tablas `Preconsultas` e `ImagenesIris` se crearon directamente en Somee y no tienen script en el repositorio. Conviene generarlo.

**Para saber qué código de trigger está vivo ahora mismo**, la fuente de verdad no es el repositorio sino la base:

```sql
SELECT tr.name, OBJECT_NAME(tr.parent_id) AS Tabla, m.definition
FROM sys.triggers tr
JOIN sys.sql_modules m ON m.object_id = tr.object_id
WHERE tr.parent_id > 0;
```

---

## 14. Consultas útiles de inspección

### Ver todas las columnas calculadas y su fórmula

```sql
SELECT OBJECT_NAME(cc.object_id) AS Tabla, cc.name AS Columna,
       cc.definition AS Formula, cc.is_persisted AS Persistida
FROM sys.computed_columns cc
ORDER BY Tabla, Columna;
```

### Ver todos los índices filtrados con su filtro

```sql
SELECT OBJECT_NAME(i.object_id) AS Tabla, i.name AS Indice,
       i.is_unique AS EsUnico, i.filter_definition AS Filtro
FROM sys.indexes i
WHERE i.has_filter = 1;
```

### Ver todas las llaves foráneas y su comportamiento al borrar

```sql
SELECT fk.name AS Restriccion,
       OBJECT_NAME(fk.parent_object_id) AS TablaHija,
       OBJECT_NAME(fk.referenced_object_id) AS TablaPadre,
       fk.delete_referential_action_desc AS AlBorrar
FROM sys.foreign_keys fk
ORDER BY TablaHija;
```

### Verificar que no hay solapes de médico (debe devolver cero filas)

```sql
SELECT a.CitaId AS CitaA, b.CitaId AS CitaB, a.MedicoId
FROM dbo.Citas a
JOIN dbo.Citas b
  ON b.MedicoId = a.MedicoId
 AND b.CitaId > a.CitaId
 AND a.BloqueaHorario = 1 AND b.BloqueaHorario = 1
 AND a.FechaHoraInicio < b.FechaHoraFin
 AND b.FechaHoraInicio < a.FechaHoraFin;
```

### Verificar que el `CHECK` de la tercera reprogramación se respeta

```sql
SELECT CitaId, NumeroReprogramaciones, AutorizacionTerceraPorUsuarioId
FROM dbo.Citas
WHERE NumeroReprogramaciones >= 3
  AND AutorizacionTerceraPorUsuarioId IS NULL;
```

Si cualquiera de las dos últimas devuelve filas, hay una regla de negocio violada en los datos.
