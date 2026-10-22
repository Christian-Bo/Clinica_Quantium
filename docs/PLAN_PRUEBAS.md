# Plan de pruebas — Clínica Quantium

Estrategia de pruebas del backend: qué se prueba, con qué herramienta, y el catálogo de casos manuales que la suite automatizada no cubre.

**Elaborado por:** Allan Ricardo Quiej Ixmatá — Analista funcional / QA
**Rama de referencia:** `develop` @ `43d8791`
**Estado de la suite:** 183 casos unitarios y 24 de integración, en verde

---

## 1. Estrategia

El sistema se prueba en tres niveles, y cada uno existe porque los otros dos no pueden cubrir lo que él cubre.

| Nivel | Qué verifica | Con qué | Cuánto tarda | Necesita base de datos |
|---|---|---|---|---|
| Unitario | Reglas de negocio puras: máquina de estados, rangos de validación, cálculos | xUnit sobre `Domain` y `Application` | ~150 ms | No |
| Integración | Que cada ruta responda el código de autorización correcto | xUnit con `WebApplicationFactory` | ~2 s, o ~50 s si la base no responde | Sí |
| Manual | Flujos completos, mensajes de error legibles, carga de archivos | Swagger y DBeaver | ~1 h | Sí |

La proporción es deliberada: la mayoría del valor está en el nivel unitario, porque ahí vive la lógica que puede romperse sin que nadie se dé cuenta.

### Por qué las pruebas unitarias no tocan la base

`ClinicaPro.Domain` no referencia Entity Framework, SQL Server ni ASP.NET Core. Eso no es casualidad ni preferencia estética: es lo que permite que 183 casos corran en 150 milisegundos. Una suite que tarda medio segundo se ejecuta en cada guardado; una que tarda un minuto se ejecuta una vez al día, y deja de servir.

Hay una prueba que protege esa propiedad: `DomainAssemblyTests.cs` verifica que el ensamblado de dominio no tenga dependencias prohibidas. Si alguien agrega un paquete de acceso a datos a ese proyecto, la prueba falla y el error explica por qué.

### Por qué no se usa una biblioteca de simulación

El proyecto de pruebas unitarias **no usa Moq ni NSubstitute**. Los dobles de prueba están escritos a mano como clases que implementan la interfaz del repositorio y devuelven listas en memoria.

Es una decisión con ventajas y costos. A favor: una dependencia menos, y los dobles se leen como código normal en lugar de como una cadena de configuración. En contra: escribir un doble nuevo cuesta más que una línea de `Mock.Of<T>()`. Para el tamaño de este proyecto la decisión se sostiene; en uno con treinta repositorios no se sostendría.

---

## 2. Cómo ejecutar la suite

```
cd C:\Clinica_Quantium
dotnet build ClinicaPro.sln
dotnet test ClinicaPro.sln
```

Para correr solo las unitarias, que es lo habitual durante el desarrollo:

```
dotnet test tests/ClinicaPro.UnitTests
```

Para un solo archivo o una sola prueba:

```
dotnet test --filter "FullyQualifiedName~CitaTests"
dotnet test --filter "FullyQualifiedName~Cancelar_ConMenosDeDosHoras_PasaANoPresentada"
```

Salida esperada:

```
Passed!  - Failed: 0, Passed: 183, Skipped: 0, Total: 183 - ClinicaPro.UnitTests.dll
Passed!  - Failed: 0, Passed:  24, Skipped: 0, Total:  24 - ClinicaPro.IntegrationTests.dll
```

> **Sobre el conteo:** hay 143 métodos de prueba, pero se ejecutan 183 casos. La diferencia son los `[Theory]` con varios `[InlineData]`, que generan un caso por cada juego de datos. Los más usados están en `ModeloDatosPacienteTests` con 8 teorías.

### Si las pruebas de integración fallan

Las 24 pruebas de integración levantan la aplicación real con `WebApplicationFactory`, y esa aplicación se conecta a la base de Somee. **No hay base en memoria ni contenedor.**

Síntoma típico: `Disponibilidad_WithoutToken_ReturnsOk` falla con `InternalServerError` después de 40 a 50 segundos.

Ese tiempo es el diagnóstico. En `DependencyInjection.cs` la conexión está configurada con `EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: 5s)`, así que tres reintentos contra un servidor inalcanzable dan exactamente ese rango. **No es un error de código: es la red.** Muchas redes institucionales y corporativas bloquean el puerto 1433 de salida.

Verificación en treinta segundos:

```
dotnet run --project src/ClinicaPro.Api --launch-profile https
```

y abrir `https://localhost:7041/api/health/database`. Si tarda y falla, confirmado. La solución práctica es usar otra red.

**Esta dependencia es la debilidad conocida de la suite** y está anotada como tal: una prueba que puede fallar por razones ajenas al código entrena al equipo a ignorar los resultados en rojo. La corrección correcta sería levantar SQL Server en un contenedor para las pruebas, o usar el proveedor en memoria de EF Core para las que no verifican comportamiento de SQL Server.

---

## 3. Cobertura por área

### Dominio — la máquina de estados

`CitaTests.cs` — 19 métodos. Es el archivo más importante de la suite.

| Prueba | Qué garantiza |
|---|---|
| `Solicitar_ConDatosValidos_QuedaSolicitada` | El estado inicial es correcto |
| `Solicitar_MotivoCorto_LanzaExcepcionDeDominio` | El motivo exige al menos 5 caracteres |
| `Solicitar_FechaHoraPasada_LanzaExcepcionDeDominio` | No se agenda hacia atrás |
| `ExigirNoPasado_HoraDeHoyYaTranscurrida_Lanza` | Compara hora, no solo fecha |
| `ExigirNoPasado_UnMinutoEnElFuturo_NoLanza` | El límite es exacto, no hay margen arbitrario |
| `ConfirmarPorSecretaria_DesdeSolicitada_PasaAProgramada` | La transición feliz |
| `ConfirmarPorSecretaria_DesdeOtroEstado_LanzaExcepcion` | No se confirma dos veces |
| `RegistrarLlegada_DesdeProgramada_PasaAEnEspera` | Se puede saltar `Confirmada` |
| `FlujoRecepcion_HastaAtendida_RespetaLaMaquinaDeEstados` | El recorrido completo de punta a punta |
| `Cancelar_ConMasDeDosHoras_PasaACancelada` | Cancelación con anticipación |
| `Cancelar_ConMenosDeDosHoras_PasaANoPresentada` | **Cancelar tarde cuenta como inasistencia** |
| `CancelarPorPaciente_Programada_PasaACancelada` | El paciente puede cancelar antes de confirmar |
| `CancelarPorPaciente_Confirmada_LanzaExcepcionDeDominio` | **Pero no después** |
| `Cancelar_Staff_Confirmada_PasaACancelada` | El personal sí puede |
| `Reprogramar_CambiaHorarioYCuenta` | Sube el contador |
| `Reprogramar_AFechaPasada_LanzaExcepcionDeDominio` | Tampoco hacia atrás |
| `Reprogramar_TerceraSinAdmin_LanzaExcepcion` | La tercera exige autorización |
| `Reprogramar_TerceraConAdmin_QuedaAutorizada` | Con autorización procede y queda registrada |
| `DiaSemana_LunesEsUnoYDomingoEsSiete` | La conversión de `DayOfWeek` de .NET a la numeración de la base |

Las dos pruebas de cancelación en negrita son las que capturan la regla de negocio menos obvia del sistema y la que más confunde a quien lo ve por primera vez.

### Dominio — captura clínica

`PreconsultaTests.cs` — 9 métodos. Cubre los cuatro rangos de signos vitales, la regla cruzada de que la diastólica sea menor que la sistólica, la actualización y la baja lógica doble.

`ImagenIrisTests.cs` — 10 métodos. Cubre la lista blanca de lateralidades y su normalización, la lista blanca de tipos de contenido, el archivo vacío, el archivo que supera 10 MB, y la baja lógica doble.

`ExpedienteTests.cs` — 6 métodos, de los cuales estos tres cubren reglas que no existían antes del PR #39:

| Prueba | Qué garantiza |
|---|---|
| `PermiteCapturaClinica_SoloSala` | Solo `En Espera` y `En Atencion` permiten captura clínica |
| `RegistrarPreconsulta_EnProgramada_Lanza` | No se toman signos antes de que el paciente llegue |
| `SubirImagenIris_EnAtendida_Lanza` | No se adjuntan imágenes después de cerrar la consulta |
| `AccesoExpediente_MedicoSinCitas_LanzaForbidden` | Un médico no ve expedientes de pacientes ajenos |
| `ExpedientePaciente_MedicoConCita_ListaSignosSinBinario` | El listado no arrastra el binario de las imágenes |

La última merece atención: verifica una decisión de rendimiento, no de corrección funcional. Es el tipo de prueba que evita que alguien "simplifique" el repositorio devolviendo la entidad completa y multiplique por mil el tamaño de la respuesta.

### Aplicación — servicios

| Archivo | Casos | Qué cubre |
|---|---|---|
| `ValidarAgendaPacienteServiceTests.cs` | 4 | Solape del paciente consigo mismo, el tope de 3 citas activas futuras, y que reprogramar la misma cita no cuente como solape |
| `SolicitarCitaServiceTests.cs` | 5 | Resolución del médico, lectura de la duración desde parámetros |
| `ListarAgendaServiceTests.cs` | 5 | Que un médico no pueda ver agenda ajena, que el personal sí pueda filtrar, y la propagación del nuevo filtro por estado |
| `HistorialMedicoPacienteServiceTests.cs` | 3 | Armado del contexto clínico |
| `PacienteBajaPorInasistenciaTests.cs` | 4 | Las dos condiciones de la baja |
| `BuscarPacientesServiceTests.cs` | 2 | Paginación del buscador |
| `ListarNotificacionesStaffServiceTests.cs` | 2 | Filtros de la cola |
| `ActualizarPerfilPacienteServiceTests.cs` | 3 | Actualización del perfil propio |

La prueba `ExecuteAsync_Medico_IgnoraMedicoIdAjenoYUsaElPropio` es de seguridad, no de funcionalidad: comprueba que un médico que manipule el parámetro `medicoId` de la petición no pueda leer la agenda de un colega.

### Dominio — entidades y catálogos

| Archivo | Casos | Qué cubre |
|---|---|---|
| `ModeloDatosPacienteTests.cs` | 27 | Validación de todos los campos del paciente. El archivo más grande, casi todo en `[Theory]` |
| `PacienteTests.cs` | 4 | Creación, actualización y normalización de sexo |
| `MedicoTests.cs` | 6 | Número de colegiado obligatorio |
| `NotificacionTests.cs` | 4 | Estados y transiciones de la cola |
| `AutorizacionReprogramacionTests.cs` | 4 | La reconstrucción desde `HistorialCitas` |
| `ClinicaMarcaTests.cs` | 7 | Que el nombre oficial sea consistente y no queden literales viejos |
| `MatrizCompatibilidadRolesTests.cs` | 3 | Combinaciones de roles permitidas |
| `RolesStaffAdministrablesTests.cs` | 4 | Qué roles puede asignar un administrador |
| `TipoClinicoUsuarioTests.cs` | 4 | Resolución del perfil clínico |
| `MustChangePasswordAccessTests.cs` | 2 | Qué rutas se permiten con la bandera puesta |
| `CitaAccesoMedicoTests.cs` | 2 | Verificación de médico asignado |
| `HistorialCitaNarrativaTests.cs` | 3 | Texto legible del historial |
| `DomainAssemblyTests.cs` | 1 | Que el dominio no tenga dependencias de infraestructura |

### Integración — autorización

24 pruebas que verifican una sola cosa por ruta: que sin token la respuesta sea `401` y no `200`.

Es una cobertura deliberadamente estrecha. No prueban que la lógica funcione —para eso están las unitarias— sino que **nadie quitó un atributo `[Authorize]` por accidente**. Es el tipo de error que no rompe ninguna otra prueba y que no se nota hasta que alguien lo explota.

La excepción es `Disponibilidad_WithoutToken_ReturnsOk`, que verifica lo contrario: que ese endpoint **sí** responda `200` sin token, porque es anónimo a propósito. Si alguien le agregara `[Authorize]`, esa prueba lo detectaría.

`AuthEndpointTests.cs` agrega 5 casos sobre el registro: que sin los datos de la primera cita devuelva `400`, y que con un `medicoId` vacío también.

---

## 4. Catálogo de pruebas manuales

Lo que la suite automatizada no cubre: los flujos completos de punta a punta, la legibilidad de los mensajes de error, y la carga de archivos.

### Preparación

1. Poner el repositorio al día y confirmar que compila.
2. Levantar el API con `dotnet run --project src/ClinicaPro.Api --launch-profile https`.
3. Abrir `https://localhost:7041/swagger`.
4. Verificar `GET /api/health/database` antes de cualquier otra cosa.
5. Obtener tokens de tres roles distintos. Swagger guarda uno solo, así que conviene usar tres ventanas del navegador.

> Al pulsar **Authorize**, pegar **solo el token**, sin escribir `Bearer`.

### Caso CP-01 — Ciclo de vida completo

Fecha de prueba: la semana siguiente, en formato `2026-10-27T09:00:00`, **sin la `Z` final**.

| Paso | Llamada | Rol | Esperado |
|---|---|---|---|
| 1 | `GET /api/citas/disponibilidad?fecha=2026-10-27` | Anónimo | `200` con cupos. Vacío significa que ningún médico tiene horario ese día |
| 2 | `POST /api/citas/para-paciente` | Secretaria | `201`. Estado `Solicitada`. Guardar el `citaId` |
| 3 | `GET /api/citas/pendientes` | Secretaria | La cita aparece en la cola |
| 4 | `POST /api/citas/{id}/confirmar` | Secretaria | `200`. Estado `Programada` |
| 5 | `POST /api/citas/{id}/confirmar-asistencia` | Paciente | `200`. Estado `Confirmada` |
| 6 | `POST /api/citas/{id}/llegada` | Secretaria | `200`. Estado `En Espera` |
| 7 | `PUT /api/citas/{id}/preconsulta` con `120/80`, `36.6`, `98` | Secretaria | `200` |
| 8 | Repetir el paso 7 con sistólica `130` | Secretaria | `200`, y en la base **sigue habiendo una sola** preconsulta activa |
| 9 | `POST /api/citas/{id}/imagenes-iris` con un `.jpg` y `lateralidad = OI` | Secretaria | `201`, con `tamanoBytes` calculado por el motor |
| 10 | `POST /api/citas/{id}/iniciar` | Medico | `200`. Estado `En Atencion` |
| 11 | `POST /api/citas/{id}/finalizar` | Medico | `200`. Estado `Atendida` |
| 12 | `GET /api/citas/{id}/historial` | Cualquiera | La bitácora completa, con actor y motivo en cada entrada |

**Nota sobre el orden.** Los pasos 7 y 9 tienen que ir después del 6. Desde el PR #39, la preconsulta y las imágenes solo se aceptan con la cita en `En Espera` o `En Atencion`. Intentarlos antes devuelve `400`, y eso es correcto.

El paso 12 es el más demostrativo: enseña que el sistema registró seis cambios de estado sin que ninguna línea de código escribiera en la tabla de historial.

### Caso CP-02 — Pruebas negativas

Todas deben devolver **`400` con mensaje legible en español**, nunca un `500` ni un error SQL crudo.

| # | Qué intentar | Mensaje esperado | Dónde se valida |
|---|---|---|---|
| 01 | Motivo de 3 letras | "El motivo de consulta debe tener al menos 5 caracteres." | Dominio |
| 02 | Fecha de ayer | "No se permite crear o reprogramar una cita en una fecha u hora pasada." | Dominio, y trigger 51000 de respaldo |
| 03 | Dos citas en el mismo cupo del mismo médico | "Existe otra solicitud o cita activa que se solapa..." o "Ya existe una cita activa en ese horario." | Trigger 51003 o índice único |
| 04 | Dos citas del mismo paciente a la misma hora, con médicos distintos | "El paciente ya tiene otra cita activa que se solapa..." | Servicio, y trigger 51006 |
| 05 | Cita a las 23:00, fuera del horario del médico | "La cita está fuera del horario laboral configurado para el médico." | Trigger 51002 |
| 06 | Cuarta cita activa futura del mismo paciente | "No puede tener más de 3 citas activas." | `ValidarAgendaPacienteService` |
| 07 | `confirmar` una cita ya `Programada` | "Solo una cita Solicitada puede pasar a Programada." | Dominio |
| 08 | `iniciar` sin pasar por `llegada` | "Solo una cita En Espera puede iniciar atención." | Dominio |
| 09 | `cancelar` como paciente una cita `Confirmada` | "Una cita confirmada no puede cancelarse desde el portal del paciente..." | Dominio |
| 10 | Cuarta reprogramación | "La cita ya alcanzó el máximo de reprogramaciones." | Dominio y `CHECK` |
| 11 | Tercera reprogramación sin autorización | "La tercera reprogramación requiere autorización de un Administrador." | Dominio y trigger 51004 |
| 12 | Preconsulta con presión `300/80` | "La presión sistólica debe estar entre 60 y 250." | Dominio |
| 13 | Preconsulta con `80/120` | Error de validación cruzada | Dominio |
| 14 | Preconsulta sobre una cita `Programada` | "Solo se pueden registrar signos vitales cuando la cita está En Espera o En Atencion." | `PreconsultaServices` |
| 15 | Iris sobre una cita `Atendida` | "Solo se pueden adjuntar imágenes de iris cuando la cita está En Espera o En Atencion." | `ImagenIrisServices` |
| 16 | Iris con `lateralidad = XYZ` | "La lateralidad debe ser OD, OI, Ambos, No especificada." | Dominio |
| 17 | Iris con un archivo `.pdf` | Tipo de contenido no permitido | Dominio |
| 18 | Iris de más de 10 MB | Tamaño excedido | Dominio |
| 19 | Dos pacientes con el mismo DPI | "Ya existe un paciente con ese documento." | Índice `UQ_Pacientes_Documento`, error 2601 traducido |
| 20 | Paciente con `sexo = Z` | "El sexo debe ser M, F o X." | Dominio |
| 21 | Agenda con `hasta` anterior a `desde` | "El rango de agenda es inválido." | Servicio |

**Casos de autorización**, que deben devolver `401` o `403`:

| # | Qué intentar | Esperado |
|---|---|---|
| 22 | Cualquier endpoint sin token | `401` |
| 23 | `/api/admin/usuarios` con token de paciente | `403` |
| 24 | `iniciar` una cita asignada a otro médico | `403` |
| 25 | Expediente de un paciente con el que el médico no tiene citas | `403` |
| 26 | Agenda con `medicoId` de otro médico, usando token de médico | `200`, pero devuelve **su propia agenda**, no la ajena |

El caso 26 es el más sutil: no falla, filtra. Un rechazo sería más visible pero menos útil; lo importante es que el dato ajeno no se devuelva.

> **Advertencia práctica de Swagger:** en los campos de texto opcionales queda escrita la palabra `string`. Si no se borra, el API la recibe como valor real y devuelve un `400` que parece un error del sistema pero es culpa del formulario. Pasa sobre todo con `lateralidad` y `observacion`.

### Caso CP-03 — Verificación en la base

Después de ejecutar CP-01, abrir DBeaver y confirmar con SQL lo que la API dijo que hizo.

| # | Verificación | Resultado esperado |
|---|---|---|
| 1 | La fila de la cita con su estado final | `Atendida`, `BloqueaHorario = 0` |
| 2 | `HistorialCitas` de esa cita, **ordenado por `HistorialCitaId`** | Seis entradas con actor y motivo |
| 3 | Solapes de médico entre citas activas | **Cero filas** |
| 4 | Solapes del mismo paciente entre citas activas | **Cero filas** |
| 5 | Citas activas fuera del horario de su médico | **Cero filas** |
| 6 | Citas con 3 reprogramaciones y sin autorización | **Cero filas** |
| 7 | Más de una preconsulta activa por cita | **Cero filas** |
| 8 | `TamanoBytes` contra `DATALENGTH(Imagen)` | Coinciden en todas las filas |

Las consultas 3 a 7 son la red de seguridad del sistema: cada fila que devuelvan es una regla de negocio violada en los datos.

> **Sobre el punto 2.** Hay que ordenar por `HistorialCitaId` y no por `FechaCambioUtc`. Varias entradas escritas en la misma transacción comparten el mismo valor de `SYSUTCDATETIME()` al segundo, así que ordenar por fecha devuelve un orden arbitrario entre ellas. Este fue un defecto real que se corrigió en el repositorio.

### Caso CP-04 — Los triggers directamente

Prueba que el historial no se puede evadir ni con acceso directo a la base.

**Paso 1.** Desde DBeaver, cambiar el estado de una cita sin establecer el contexto de sesión:

```sql
UPDATE dbo.Citas SET Estado = N'Programada' WHERE CitaId = '<guid>';
```

Esperado: `Msg 51006` — *"Para cambiar estado o reprogramar debe establecer SESSION_CONTEXT UsuarioId."* La operación falla completa.

**Paso 2.** Hacerlo correctamente, igual que lo hace la aplicación:

```sql
BEGIN TRANSACTION;
EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = '<guid de un usuario>';
EXEC sys.sp_set_session_context @key = N'MotivoCambio', @value = N'Prueba manual';

UPDATE dbo.Citas SET Estado = N'Programada' WHERE CitaId = '<guid>';

SELECT * FROM dbo.HistorialCitas WHERE CitaId = '<guid>' ORDER BY HistorialCitaId DESC;

ROLLBACK;
```

Esperado: el `UPDATE` pasa y aparece una entrada nueva en el historial con el usuario y el motivo indicados. **Terminar con `ROLLBACK`** para no alterar la base compartida.

**Paso 3.** Intentar una transición ilegal:

```sql
BEGIN TRANSACTION;
EXEC sys.sp_set_session_context @key = N'UsuarioId', @value = '<guid>';
UPDATE dbo.Citas SET Estado = N'Atendida'
WHERE CitaId = '<guid>' AND Estado = N'Solicitada';
ROLLBACK;
```

Esperado: `Msg 51005` — *"Transición de estado no permitida por las reglas de negocio."*

**Paso 4.** Intentar escribir una columna calculada:

```sql
UPDATE dbo.ImagenesIris SET TamanoBytes = 999 WHERE 1 = 0;
```

Esperado: `Msg 271` — la columna no se puede modificar porque es calculada.

Este caso es el argumento más fuerte del diseño: las reglas no dependen de que la aplicación se porte bien.

---

## 5. Criterios de aceptación

Antes de fusionar una rama a `develop`:

1. `dotnet build ClinicaPro.sln` sin errores.
2. `dotnet test ClinicaPro.sln` con las 183 unitarias en verde. Si fallan pruebas de integración, confirmar primero con `/api/health/database` que es la red y no el código.
3. Si se tocó una regla de negocio, hay una prueba unitaria nueva que la cubre.
4. Si se tocó un endpoint, se ejecutó el caso manual correspondiente en Swagger.
5. Si se tocó el esquema, el cambio está en un script de `database/` y las verificaciones de CP-03 siguen devolviendo cero filas.

---

## 6. Deuda de pruebas conocida

Lista honesta de lo que falta, en orden de importancia.

| # | Hueco | Riesgo | Corrección propuesta |
|---|---|---|---|
| 1 | Las pruebas de integración dependen de una base remota compartida | Una prueba falla por la red y el equipo se acostumbra al rojo | SQL Server en contenedor, o proveedor en memoria para las que no verifiquen comportamiento del motor |
| 2 | No hay pruebas de los triggers ni de los `CHECK` | Un cambio de esquema puede romper una regla sin que nada lo detecte | Pruebas de integración que inserten contra una base real y esperen el número de error |
| 3 | No hay pruebas de concurrencia | `RowVersion` y el índice único nunca se ejercitan bajo carga | Prueba que lance dos solicitudes simultáneas al mismo cupo y verifique que exactamente una gane |
| 4 | `Disponibilidad_WithoutToken_ReturnsOk` usa una fecha fija ya pasada | La prueba pasa devolviendo lista vacía, sin verificar nada del cálculo | Usar una fecha relativa a hoy |
| 5 | No hay pruebas del envío de notificaciones | El worker nunca se ejercita | Doble de `IEmailSender` y verificación de la cola |
| 6 | No se mide cobertura | No se sabe qué quedó sin probar | `dotnet test --collect:"XPlat Code Coverage"` |

El punto 1 es el único que conviene resolver antes de la entrega final; los demás son mejoras razonables para un sistema que siguiera en producción.
