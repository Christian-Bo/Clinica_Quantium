# Matriz de reglas de negocio — Clínica Pro

Cruce entre las reglas declaradas en el Documento de Arquitectura (sección 3.1) y su
implementación real en el código. Cada regla incluye dónde vive, cómo se verifica y los
casos de prueba que la cubren.

Elaborado por: Allan Ricardo Quiej Ixmata
Rama de referencia: `develop` @ sincronizada con `main`
Pruebas automatizadas al momento del cruce: 85 (62 unitarias + 23 de integración)

---

## Leyenda de estado

| Símbolo | Significado |
|---|---|
| ✅ | Implementada y coincide con el documento |
| ⚠️ | Implementada, pero con una diferencia respecto a lo documentado |
| ⬜ | Pendiente de verificar en ejecución |

---

## R1 — Un único médico primario activo por especialidad

> *"Una especialidad debe tener un único médico primario activo en la fase 1; el modelo admite más médicos en el futuro mediante Médico-Especialidad."*

| | |
|---|---|
| **Capa de aplicación** | `AdministrarMedicoEspecialidadesService.cs:56` y `:94` → `ExisteOtroPrimarioActivoAsync` |
| **Capa de datos** | Índice `UX_MedicoEspecialidad_PrimarioActivo` (filtrado, único) |
| **Estado** | ✅ Doble defensa: valida en servicio para dar mensaje claro, y el índice protege ante concurrencia |

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R1-01 | Asignar primer médico primario a una especialidad sin primario | 200 / 201 |
| R1-02 | Asignar un segundo médico primario a la misma especialidad | 400 con mensaje legible (no error SQL crudo) |
| R1-03 | Desactivar el primario y luego asignar otro | Permitido |

---

## R2 — Las solicitudes bloquean el horario

> *"Las solicitudes en estado Solicitada participan en la validación de disponibilidad para impedir reservas duplicadas."*

| | |
|---|---|
| **Capa de datos** | Columna calculada `BloqueaHorario` en `dbo.Citas`; usada por el trigger `TR_Citas_Validaciones` |
| **Error** | 51003 → traducido a 400 por `SqlServerExceptionMapper` |
| **Estado** | ✅ |

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
| **Capa de aplicación** | `Cita.ConfirmarPorSecretaria` (`Cita.cs:79`) |
| **Capa de datos** | El trigger `TR_Citas_Validaciones` revalida en el UPDATE |
| **Estado** | ⚠️ **Divergencia de forma** |

**Observación:** el documento describe la revalidación como una operación de la capa de
aplicación dentro de una transacción explícita. En el código, la revalidación la ejecuta el
trigger de SQL Server durante el `UPDATE`, dentro de la transacción implícita de la
sentencia. El efecto es equivalente —no se puede confirmar sobre un horario ocupado— pero
la responsabilidad está en la base y no en `Application`.

Requiere decisión del equipo: ajustar el documento para describir lo implementado, o mover
la revalidación a la capa de aplicación.

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
| **Capa de datos** | `TR_Citas_Validaciones`: 51000 (pasado), 51002 (fuera de horario), 51003 (solape) |
| **Traducción** | `SqlServerExceptionMapper` → 400 legible |
| **Estado** | ✅ |

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R4-01 | Solicitar cita con fecha anterior a hoy | 400 (51000) |
| R4-02 | Solicitar cita un sábado | 400 (51002) |
| R4-03 | Solicitar cita a las 07:00 (antes de las 08:00) | 400 (51002) |
| R4-04 | Solicitar cita a las 17:00 (después de las 16:00) | 400 (51002) |
| R4-05 | Solicitar cita superpuesta con otra activa del mismo médico | 400 (51003) |
| R4-06 | Solicitar cita en día y hora laboral libre | 201 |

---

## R5 — La tercera reprogramación requiere autorización del administrador

> *"La tercera reprogramación requiere autorización del administrador y toda reprogramación registra fecha anterior, nueva fecha, motivo y usuario."*

| | |
|---|---|
| **Capa de dominio** | `Cita.Reprogramar` (`Cita.cs:120`) |
| **Capa de aplicación** | `ReprogramarCitaService.cs:32` — crea la solicitud de autorización |
| **Capa de datos** | `CK_Citas_Reprogramaciones` (0–3), `CK_Citas_TerceraReprogramacion`, trigger 51004 (verifica rol Administrador) |
| **Trazabilidad** | Trigger `TR_Citas_Historial` registra el tipo `Reprogramacion` con usuario y motivo |
| **Estado** | ✅ |

**Conteo confirmado desde el esquema:** se permiten 3 reprogramaciones como máximo. La
primera (0→1) y la segunda (1→2) son libres; la tercera (2→3) exige
`AutorizacionTerceraPorUsuarioId` de un usuario con rol Administrador activo. Un cuarto
intento lo bloquea el CHECK.

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
| **Capa de dominio** | `Cita.Cancelar(ahoraClinica, horasMinimasAnticipacion)` (`Cita.cs:108`) |
| **Capa de aplicación** | Lee el parámetro `Citas.HorasMinimasCancelacion` |
| **Estado** | ⚠️ **Divergencia técnica** |

**Observación:** existen dos sobrecargas de `Cancelar`. La que recibe parámetros lee el
valor configurable desde la base. La sobrecarga sin parámetros (`Cita.cs:103`) tiene el
valor **2 escrito directamente en el código**:

```csharp
public void Cancelar()
{
    Cancelar(HoraClinica.Ahora(), horasMinimasAnticipacion: 2);
}
```

Si un administrador cambia `Citas.HorasMinimasCancelacion` a otro valor, cualquier ruta que
llame a la sobrecarga sin parámetros seguirá usando 2 horas. Pendiente confirmar en
ejecución si alguna ruta activa la utiliza.

**Casos de prueba**

| ID | Escenario | Resultado esperado |
|---|---|---|
| R6-01 | Cancelar con más de 2 horas de anticipación | Estado `Cancelada` |
| R6-02 | Cancelar con menos de 2 horas de anticipación | Estado `No presentada` |
| R6-03 | Cambiar el parámetro a 4 horas y cancelar con 3 horas | Estado `No presentada` (verifica que el parámetro se respeta) |
| R6-04 | Cancelar una cita ya `Atendida` | 400 |

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

## Flujo completo de estados (recorrido end-to-end)

Además de las reglas individuales, conviene verificar el recorrido completo, que hoy **no
está cubierto por ninguna prueba automatizada** — las 23 pruebas de integración existentes
verifican principalmente respuestas 401 sin token.

| ID | Paso | Actor | Estado resultante |
|---|---|---|---|
| E2E-01 | Solicitar cita | Paciente | `Solicitada` |
| E2E-02 | Confirmar solicitud | Secretaria | `Programada` |
| E2E-03 | Confirmar asistencia | Paciente | `Confirmada` |
| E2E-04 | Registrar llegada | Secretaria | `En Espera` |
| E2E-05 | Iniciar atención | Médico asignado | `En Atencion` |
| E2E-06 | Finalizar atención | Médico asignado | `Atendida` |
| E2E-07 | Consultar historial | Secretaria | 6 transiciones en orden ascendente |

---

## Resumen

| Estado | Cantidad |
|---|---|
| ✅ Coincide con el documento | 5 |
| ⚠️ Divergencia a resolver con el equipo | 2 (R3, R6) |
| Casos de prueba definidos | 31 + 7 de recorrido completo |

**Las dos divergencias no son defectos funcionales.** En ambos casos el sistema se comporta
de forma razonable; lo que no coincide es *dónde* está la responsabilidad (R3) y si el valor
es configurable en todas las rutas (R6). Requieren una decisión del equipo: ajustar el
código o ajustar el documento.

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
- `fechaHoraInicio` en hora de Guatemala, sin sufijo `Z`. Ejemplo: `2027-03-15T09:00:00`
- Solo lunes a viernes, entre 08:00 y 16:00
- La fecha debe ser futura
- Usar horarios distintos entre casos para no chocar con la validación de solape
- La base de Somee es compartida: usar correo propio y no reutilizar horarios de otros integrantes
