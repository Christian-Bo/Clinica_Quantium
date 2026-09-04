# ClinicaPro.Contracts

DTOs compartidos entre API y Client (Blazor). JSON en camelCase.

API local: `http://localhost:5041/` (perfil http) o `https://localhost:7041/`. CORS: `https://localhost:7142` y `http://localhost:5142`.

Header: `Authorization: Bearer {accessToken}`. En Swagger pegar solo el token.

`GET /api/health/database` debe devolver `server: ClinicaPro.mssql.somee.com`.

## Auth

| Método | Ruta | Auth |
|---|---|---|
| POST | `/api/auth/login` | no |
| POST | `/api/auth/register/paciente` | no (201). Body incluye primera cita: `medicoId`, `fechaHoraInicio` (Guatemala, sin Z), `motivoConsulta`. 400 si faltan. 409 si correo o DPI repetido |
| POST | `/api/auth/forgot-password` | no. Body: `email`. Siempre 200 (o 429 si hay abuso). Envía código por correo |
| POST | `/api/auth/reset-password` | no. Body: `email`, `token`, `newPassword` |
| POST | `/api/auth/change-password` | sí. Si `mustChangePassword`, es el único POST permitido junto con `GET /api/auth/me` |
| GET | `/api/auth/me` | sí |
| GET | `/api/pacientes/me` | sí |
| PUT | `/api/pacientes/me` | Paciente. Perfil + sexo, alergias, contactoEmergencia. **No cambia DPI** (sí recepción/admin) |
| PUT | `/api/pacientes/{pacienteId}` | Secretaria / Admin. Mismo body; sí puede corregir DPI |
| GET | `/api/pacientes?q=&page=1&pageSize=20` | Secretaria / Admin. `{ items, total, page, pageSize }`. pageSize máx. 50 |
| POST | `/api/pacientes` | Secretaria / Admin |

Password: 8+, mayúscula, minúscula, dígito y símbolo. `sexo`: `M`, `F`, `X` o null.

## Citas

El paciente pide con `medicoId` + `fechaHoraInicio` (Guatemala, **sin Z**) + `motivoConsulta` (≥ 5). Al **registrarse** (`POST /api/auth/register/paciente`) esos tres campos van en el mismo body: no se crea la cuenta si no agenda la primera cita.

Secretaria/Admin también: `POST /api/citas/para-paciente` con `pacienteId` extra. `POST /api/pacientes` crea el paciente **sin** exigir cita (mostrador).

`CitaDto` incluye `pacienteNombre` y `medicoNombre`.

`GET /api/citas/disponibilidad?fecha=2026-09-11` (público, sin token). Slots de todos los médicos activos: hora + nombre del médico, sin datos de pacientes.

Un paciente no puede tener dos citas activas que se solapen (cualquier médico) ni más de **3** citas activas futuras.

Estados: Solicitada → Programada → Confirmada → En Espera → En Atencion → Atendida. Alternativas: Rechazada, Cancelada, No presentada.

`POST /api/citas/{id}/llegada` acepta **Programada o Confirmada**.

`POST /api/citas/{id}/reprogramar`. Las dos primeras las hace Secretaria. La tercera: `POST /api/citas/{id}/solicitar-autorizacion-reprogramacion`, Admin lista/aprueba/rechaza en `/api/admin/autorizaciones-reprogramacion`. Tras aprobar, Secretaria reprograma y el historial guarda quién autorizó. Un Administrador puede reprogramar la tercera directo. Cancelar con menos de 2 h → `No presentada`. El máximo es **3**, no es parámetro editable.

`GET /api/citas/{id}/historial` trae `descripcion` en español (quién, de/hacia, horas).

`GET /api/citas/paciente/{pacienteId}/historial-medico` (Médico): contexto básico del paciente (nombre, sexo, alergias) y citas **de ese médico** con el paciente. 403 si nunca lo atendió. No es expediente clínico.

`POST /api/citas/{id}/iniciar` y `finalizar`: el médico autenticado debe ser el asignado; si no, 403. Un Administrador sí puede.

`GET /api/citas/agenda`: Secretaria/Admin pueden filtrar con `medicoId`. Un Médico **ignora** `medicoId` ajeno y solo ve la suya. `GET /api/citas/medico` sigue resolviendo al usuario autenticado.

Aviso de llegada: `POST /api/citas/{id}/llegada` publica SignalR `pacienteLlego` en `/hubs/agenda-medico` (JWT en `access_token`). El doctor conectado recibe `{ citaId, pacienteId, pacienteNombre, mensaje, fechaHoraInicio }`.

## Admin (`/api/admin`, solo Administrador)

Especialidades crear/editar. `GET /api/admin/medicos` lista activos e inactivos (`isActive`). Médicos crear/editar. Especialidades del médico: GET/POST/PUT/DELETE `/api/admin/medicos/{id}/especialidades` (un primario activo por especialidad). Horarios crear/editar (`PUT .../horarios/{horarioId}` con `vigenteDesde`/`vigenteHasta`)/desactivar. Usuarios listar y activar/desactivar (no se desactiva un admin). `POST /api/admin/usuarios` crea Secretaria o Administrador. `PUT /api/admin/usuarios/{id}/roles` cambia esos roles. Médico se crea en `POST /api/admin/medicos`. Autorizaciones de 3.ª reprogramación: GET/aprobar/rechazar. Parámetros PUT valor (no `Citas.MaximoReprogramaciones`). `GET /api/admin/auditoria`.

## Reportes y notificaciones

`GET /api/reportes/citas?desde=&hasta=&medicoId=`

`GET /api/notificaciones/mias` (Paciente) y `GET /api/notificaciones?estado=&desde=&hasta=` (staff). `estado`: Pendiente, Procesando, Enviada, Fallida. Fechas en hora de Guatemala, sin Z; tope 100. Correo SMTP; no hay WhatsApp.

En el VPS: variables `Smtp__UserName` y `Smtp__Password`. Ver `src/ClinicaPro.Api/README.md`.

## Cuentas demo

Tras `POST /api/demo/preparar-agenda`:

- `admin@clinica.com` / `Admin123!`
- `secretaria@clinica.com` / `Secretaria123!`
- `medico@clinica.com` / `Medico123!` (Carlos Hernandez)
- `medico2@clinica.com` / `Medico123!` (Ana Morales)

Medicina General: `20000000-0000-0000-0000-000000000001`. Horario lun–vie 08:00–16:00. Agenda por médico (staff): `GET /api/citas/agenda?medicoId=`. El médico usa la misma ruta o `GET /api/citas/medico`; no puede ver la agenda de otro.
