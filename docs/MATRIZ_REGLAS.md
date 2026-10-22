# Matriz de reglas de negocio — Clínica Quantium

Cruce entre las reglas declaradas en el Documento de Arquitectura (sección 3.1) y su
implementación real en el código. Cada regla incluye dónde vive, cómo se verifica y los
casos de prueba que la cubren.

Elaborado por: Allan Ricardo Quiej Ixmatá
Rama de referencia: `develop` @ `43d8791`
Pruebas automatizadas al momento del cruce: 207 (183 unitarias + 24 de integración)

> **Actualización de octubre.** Este documento se revisó tras los PR #33 al #40. Los cambios
> respecto de la versión anterior: la regla R1 quedó retirada porque el modelo ya no tiene
> especialidades (ADR-07), la divergencia de R3 sigue abierta, la de R6 sigue abierta, y se
> agregaron dos reglas nuevas, R8 y R9, que antes no estaban documentadas.

---

## Leyenda de estado

| Símbolo | Significado |
|---|---|
| ✅ | Implementada y coincide con el documento |
| ⚠️ | Implementada, pero con una diferencia respecto a lo documentado |
| ⛔ | Retirada: la regla ya no aplica al modelo actual |

---

## R1 — Un único médico primario activo por especialidad

> *"Una especialidad debe tener un único médico primario activo en la fase 1; el modelo admite más médicos en el futuro mediante Médico-Especialidad."*

| | |
|---|---|
| **Estado** | ⛔ **Retirada** |

**Por qué.** El equipo eliminó las especialidades del modelo. Se borraron las tablas
`Especialidades` y `MedicoEspecialidad`, la columna `Citas.EspecialidadId` y el índice
`UX_MedicoEspecialidad_PrimarioActivo` que daba soporte a esta regla.

El motivo: en la fase 1 la clínica tiene pocos médicos y cada especialidad terminaba con uno
solo, así que el paso de elegir especialidad no aportaba información. Además la restricción
de un único primario activo bloqueaba casos reales de cobertura entre colegas.

Hoy el paciente elige fecha, hora y médico entre los cupos disponibles. El detalle completo
de la decisión está en `DECISIONES_ARQUITECTURA.md`, ADR-07.

**Acción pendiente:** actualizar la sección 3.1 del Documento de Arquitectura, que todavía
describe esta regla.

---

## R2 — Las solicitudes bloquean el horario

> *"Las solicitudes en estado Solicitada participan en la validación de disponibilidad para impedir reservas duplicadas."*

| | |
|---|---|
| **Capa de datos** | Columna calculada `BloqueaHorario` en `dbo.Citas`; usada por el trigger `TR_Citas_Validaciones` y por el índice `UX_Citas_Medico_HorarioActivo` |
| **Capa de dominio** | `CitaEstados.BloqueaHorario(estado)` replica la misma lista |
| **Error** | 51003 → traducido a 400 por `SqlServerExceptionMapper` |
| **Estado** | ✅ |

**Por qué importa.** Si las solicitudes no bloquearan, dos pacientes podrían pedir el mismo
cupo y la secretaria tendría que rechazar a uno después de haberle dado una expectativa.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R2-01 | Solicitar cita en un horario libre | 201 |
| R2-02 | Solicitar cita en el mismo horario y médico, estando la primera en `Solicitada` | 400 "horario ocupado" |
| R2-03 | Rechazar la primera y volver a solicitar el mismo horario | 201 (el horario se liberó) |

---

## R3 — Revalidación transaccional al confirmar

> *"La confirmación de secretaría revalida el horario dentro de una transacción antes de cambiar a Programada."*

| | |
|---|---|
| **Capa de aplicación** | `Cita.ConfirmarPorSecretaria` |
| **Capa de datos** | El trigger `TR_Citas_Validaciones` revalida en el `UPDATE` |
| **Estado** | ⚠️ **Divergencia de forma, sin resolver** |

**Observación:** el documento describe la revalidación como una operación de la capa de
aplicación dentro de una transacción explícita. En el código, la revalidación la ejecuta el
trigger de SQL Server durante el `UPDATE`, dentro de la transacción implícita de la
sentencia. El efecto es equivalente —no se puede confirmar sobre un horario ocupado— pero
la responsabilidad está en la base y no en `Application`.

Requiere decisión del equipo: ajustar el documento para describir lo implementado, o mover
la revalidación a la capa de aplicación. **Recomendación:** ajustar el documento. La
validación en la base es más fuerte porque también protege contra escrituras que no pasen
por el API.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R3-01 | Confirmar una solicitud cuyo horario sigue libre | 200, estado `Programada` |
| R3-02 | Ocupar el horario con otra cita y luego confirmar la solicitud original | 400, la cita permanece `Solicitada` |

---

## R4 — No se permite cita en el pasado, fuera de horario o superpuesta

> *"No se permite una cita en el pasado, fuera del horario del médico o superpuesta con otra cita activa."*

| | |
|---|---|
| **Capa de datos** | `TR_Citas_Validaciones`: 51000 (pasado), 51002 (fuera de horario), 51003 (solape de médico), 51006 (solape de paciente) |
| **Capa de aplicación** | `ValidarAgendaPacienteService` adelanta la verificación de solape del paciente |
| **Traducción** | `SqlServerExceptionMapper` → 400 legible |
| **Estado** | ✅ |

**Ampliación de octubre.** El script `ClinicaPro_CitasIntegridad.sql` agregó una regla que el
documento no contemplaba: tampoco puede haber dos citas activas del **mismo paciente** que se
solapen, aunque sean con médicos distintos. Usa el error 51006.

> **Colisión de numeración conocida.** El 51006 se usa para dos reglas distintas: solape de
> paciente en `TR_Citas_Validaciones` y falta de `SESSION_CONTEXT` en `TR_Citas_Historial`.
> Funciona porque los mensajes difieren, pero cualquier código que decida por número no
> puede distinguirlas. Propuesta: reasignar una de las dos a 51007.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R4-01 | Solicitar cita con fecha anterior a hoy | 400 (51000) |
| R4-02 | Solicitar cita un sábado | 400 (51002) |
| R4-03 | Solicitar cita a las 07:00 (antes de las 08:00) | 400 (51002) |
| R4-04 | Solicitar cita a las 17:00 (después de las 16:00) | 400 (51002) |
| R4-05 | Solicitar cita superpuesta con otra activa del mismo médico | 400 (51003) |
| R4-06 | Solicitar cita superpuesta con otra del mismo paciente, con médico distinto | 400 (51006) |
| R4-07 | Solicitar cita en día y hora laboral libre | 201 |

---

## R5 — La tercera reprogramación requiere autorización del administrador

> *"La tercera reprogramación requiere autorización del administrador y toda reprogramación registra fecha anterior, nueva fecha, motivo y usuario."*

| | |
|---|---|
| **Capa de dominio** | `Cita.Reprogramar` |
| **Capa de aplicación** | `ReprogramarCitaService` — crea la solicitud de autorización |
| **Capa de datos** | `CK_Citas_Reprogramaciones` (0–3), `CK_Citas_TerceraReprogramacion`, trigger 51004 (verifica rol Administrador) |
| **Trazabilidad** | Trigger `TR_Citas_Historial` registra el tipo `Reprogramacion` con usuario y motivo |
| **Estado** | ✅ |

**Conteo confirmado desde el esquema:** se permiten 3 reprogramaciones como máximo. La
primera (0→1) y la segunda (1→2) son libres; la tercera (2→3) exige
`AutorizacionTerceraPorUsuarioId` de un usuario con rol Administrador activo. Un cuarto
intento lo bloquea el `CHECK`.

**Detalle de implementación.** La cola de solicitudes de autorización **no tiene tabla
propia**: se guarda como filas de `HistorialCitas` con `TipoCambio = 'Autorizacion'` y la
entidad está excluida del modelo con `modelBuilder.Ignore<AutorizacionReprogramacion>()`.
Ver ADR-08.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R5-01 | Primera reprogramación | 200, contador = 1 |
| R5-02 | Segunda reprogramación | 200, contador = 2 |
| R5-03 | Tercera reprogramación sin autorización | 400 "requiere autorización de un Administrador" |
| R5-04 | Tercera reprogramación con autorización de Administrador | 200, contador = 3 |
| R5-05 | Cuarta reprogramación | 400 "alcanzó el máximo" |
| R5-06 | Reprogramar una cita ya `Atendida` | 400 |
| R5-07 | Reprogramar a la misma fecha actual | 400 "debe ser distinta" |
| R5-08 | Verificar el historial tras reprogramar | Registro con tipo `Reprogramacion`, usuario y motivo |

---

## R6 — Cancelar con menos de dos horas cuenta como No presentada

> *"Una cancelación con menos de dos horas se registra como No presentada."*

| | |
|---|---|
| **Capa de dominio** | `Cita.Cancelar(ahoraClinica, horasMinimasAnticipacion)` |
| **Capa de aplicación** | Lee el parámetro `Citas.HorasMinimasCancelacion` |
| **Estado** | ⚠️ **Divergencia técnica, sin resolver** |

**Observación:** existen dos sobrecargas de `Cancelar`. La que recibe parámetros lee el
valor configurable desde la base. La sobrecarga sin parámetros tiene el valor **2 escrito
directamente en el código**:

```csharp
public void Cancelar()
{
    Cancelar(HoraClinica.Ahora(), horasMinimasAnticipacion: 2);
}
```

Si un administrador cambia `Citas.HorasMinimasCancelacion` a otro valor, cualquier ruta que
llame a la sobrecarga sin parámetros seguirá usando 2 horas.

**Verificación realizada:** las rutas activas del API usan la sobrecarga con parámetros, a
través de `CancelarCitaService`. La sobrecarga sin argumentos quedó solo para las pruebas.
**Recomendación:** eliminarla, para que no haya dos fuentes de verdad.

**Regla relacionada que el documento no menciona.** `CancelarPorPaciente` bloquea la
cancelación si la cita está `Confirmada`, con el mensaje *"Una cita confirmada no puede
cancelarse desde el portal del paciente. Comuníquese con la clínica."* El personal sí puede,
con `cancelar-administrativa`.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R6-01 | Cancelar con más de 2 horas de anticipación | Estado `Cancelada` |
| R6-02 | Cancelar con menos de 2 horas de anticipación | Estado `No presentada` |
| R6-03 | Cambiar el parámetro a 4 horas y cancelar con 3 horas | Estado `No presentada` (verifica que el parámetro se respeta) |
| R6-04 | Cancelar una cita ya `Atendida` | 400 |
| R6-05 | Cancelar como paciente una cita `Confirmada` | 400 con el mensaje del portal |

---

## R7 — Solo el médico asignado puede marcar la cita como Atendida

> *"Solo el médico asignado puede marcar la cita como Atendida."*

| | |
|---|---|
| **Capa de aplicación** | `CitaAccesoMedico.ExigirAsignado` + `OperarCitaService.ExecuteComoMedicoOAdminAsync` |
| **Excepción** | `ForbiddenException` → 403 |
| **Estado** | ✅ |

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R7-01 | El médico asignado finaliza su cita | 200, estado `Atendida` |
| R7-02 | Otro médico intenta finalizar esa cita | 403 |
| R7-03 | Un administrador finaliza la cita | 200 (excepción documentada en el código) |
| R7-04 | Una secretaria intenta finalizar la cita | 403 por rol |

---

## R8 — La captura clínica solo se permite con el paciente en sala

> Regla **no presente** en el documento de arquitectura. Se agregó en el PR #39.

| | |
|---|---|
| **Capa de dominio** | `CitaEstados.PermiteCapturaClinica(estado)` → verdadero solo en `En Espera` y `En Atencion` |
| **Capa de aplicación** | `PreconsultaServices` e `ImagenIrisServices` la consultan antes de escribir |
| **Capa de datos** | Sin respaldo |
| **Estado** | ✅ implementada, pero **falta agregarla al documento de arquitectura** |

**Por qué existe.** El dato tiene que corresponder a la realidad física. Los signos vitales
se toman cuando el paciente está en la clínica: ni antes de que llegue, ni después de que se
fue. Registrar una presión arterial sobre una cita programada para la semana siguiente es un
dato inventado.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R8-01 | Registrar preconsulta con la cita en `En Espera` | 200 |
| R8-02 | Registrar preconsulta con la cita en `Programada` | 400 "...cuando la cita está En Espera o En Atencion" |
| R8-03 | Subir imagen de iris con la cita en `En Atencion` | 201 |
| R8-04 | Subir imagen de iris con la cita en `Atendida` | 400 |
| R8-05 | Registrar preconsulta dos veces sobre la misma cita | 200 ambas, y **una sola** fila activa en la base |

> **Deuda conocida.** La regla de "una preconsulta activa por cita" la garantiza el servicio,
> no la base. Con dos peticiones simultáneas podrían entrar dos filas. Corrección propuesta:
> `CREATE UNIQUE INDEX UX_Preconsultas_CitaActiva ON dbo.Preconsultas (CitaId) WHERE IsActive = 1;`

---

## R9 — Tope de citas activas futuras por paciente

> Regla **no presente** en el documento de arquitectura.

| | |
|---|---|
| **Capa de aplicación** | `ValidarAgendaPacienteService.ExigirPuedeAgendarAsync` |
| **Valor** | Parámetro `Citas.MaximoActivasPorPaciente`, con 3 como valor por defecto del dominio |
| **Capa de datos** | Sin respaldo |
| **Estado** | ⚠️ **El parámetro no está sembrado en la base** |

**Observación.** `ParametrosClave` declara la clave y el servicio la consulta, pero la fila
no existe en `dbo.Parametros`. El servicio cae al valor por defecto de
`Cita.MaximoActivasFuturasPorPaciente`, que es 3, así que la regla **funciona correctamente**.
Lo que falta es que el administrador pueda ajustarla desde la pantalla de parámetros.

**Corrección:** una sola inserción.

```sql
INSERT dbo.Parametros (Clave, Valor, TipoDato, Descripcion)
VALUES (N'Citas.MaximoActivasPorPaciente', N'3', N'Int',
        N'Máximo de citas activas futuras que puede tener un paciente.');
```

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R9-01 | Paciente con 2 citas activas futuras solicita una tercera | 201 |
| R9-02 | Paciente con 3 citas activas futuras solicita una cuarta | 400 "No puede tener más de 3 citas activas" |
| R9-03 | Cancelar una de las tres y volver a solicitar | 201 |
| R9-04 | Reprogramar una cita existente teniendo 3 activas | 200 (reprogramar no cuenta como cita nueva) |

---

## Flujo completo de estados (recorrido end-to-end)

Además de las reglas individuales, conviene verificar el recorrido completo, que sigue **sin
estar cubierto por ninguna prueba automatizada**: las 24 pruebas de integración verifican
principalmente respuestas 401 sin token.

| ID | Paso | Actor | Estado resultante |
|---|---|---|---|
| E2E-01 | Solicitar cita | Paciente | `Solicitada` |
| E2E-02 | Confirmar solicitud | Secretaria | `Programada` |
| E2E-03 | Confirmar asistencia | Paciente | `Confirmada` |
| E2E-04 | Registrar llegada | Secretaria | `En Espera` |
| E2E-05 | Registrar preconsulta | Secretaria | sin cambio de estado |
| E2E-06 | Subir imagen de iris | Secretaria | sin cambio de estado |
| E2E-07 | Iniciar atención | Médico asignado | `En Atencion` |
| E2E-08 | Finalizar atención | Médico asignado | `Atendida` |
| E2E-09 | Consultar expediente de la cita | Médico asignado | Cita, paciente, preconsulta e imágenes |
| E2E-10 | Consultar historial | Secretaria | 6 transiciones en orden ascendente |

> **Nota de orden.** Los pasos E2E-05 y E2E-06 deben ir después de E2E-04. Desde el PR #39
> rige la regla R8: la captura clínica solo se acepta con la cita en `En Espera` o
> `En Atencion`.

---

## Resumen

| Estado | Cantidad |
|---|---|
| ✅ Coincide con el documento | 4 (R2, R4, R5, R7) |
| ⚠️ Divergencia a resolver con el equipo | 3 (R3, R6, R9) |
| ⛔ Retirada del modelo | 1 (R1) |
| Implementada pero no documentada en arquitectura | 2 (R8, R9) |
| Casos de prueba definidos | 43 + 10 de recorrido completo |

**Ninguna de las divergencias es un defecto funcional.** En los tres casos el sistema se
comporta de forma razonable; lo que no coincide es *dónde* está la responsabilidad (R3), si
el valor es configurable en todas las rutas (R6), y si el parámetro existe en la base (R9).
Las tres requieren una decisión del equipo, no una corrección urgente.

**Lo que sí conviene resolver antes de la entrega:** agregar R8 y R9 a la sección 3.1 del
Documento de Arquitectura, y retirar de ahí la regla R1.

---

## Datos de prueba

Cuentas demo (tras ejecutar `POST /api/demo/preparar-agenda` con token de Administrador):

| Rol | Correo | Contraseña |
|---|---|---|
| Administrador | admin@clinica.com | Admin123! |
| Secretaria | secretaria@clinica.com | Secretaria123! |
| Médico | medico@clinica.com | Medico123! |

Para paciente, registrar uno propio con `POST /api/auth/register/paciente`.

**Restricciones al construir casos:**

- `fechaHoraInicio` en hora de Guatemala, sin sufijo `Z`. Ejemplo: `2026-11-16T09:00:00`
- Solo lunes a viernes, entre 08:00 y 16:00
- La fecha debe ser futura
- Usar horarios distintos entre casos para no chocar con la validación de solape
- La base de Somee es compartida: usar correo propio y no reutilizar horarios de otros integrantes

---

## Documentos relacionados

| Documento | Qué cubre |
|---|---|
| `MODELO_DATOS.md` | Las 18 tablas, índices, triggers y columnas calculadas |
| `API_ENDPOINTS.md` | Las rutas de la API con su rol, cuerpo y errores |
| `PLAN_PRUEBAS.md` | Estrategia de pruebas y el catálogo de casos manuales |
| `DECISIONES_ARQUITECTURA.md` | Por qué el sistema está hecho así y no de otra forma |
