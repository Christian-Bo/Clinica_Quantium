# ClinicaPro.Contracts

DTOs compartidos entre API y Client (Blazor). JSON en camelCase.

API local: `http://localhost:5041/` (perfil http) o `https://localhost:7041/`. CORS: `https://localhost:7142` y `http://localhost:5142`.

Header: `Authorization: Bearer {accessToken}`. En Swagger pegar solo el token.

`GET /api/health/database` debe devolver `server: ClinicaPro.mssql.somee.com`.

## Auth

| Método | Ruta | Auth |
|---|---|---|
| POST | `/api/auth/login` | no |
| POST | `/api/auth/register/paciente` | no (201). 409 si correo o DPI repetido |
| POST | `/api/auth/forgot-password` | no. Body: `email`. Siempre 200. Envía código por correo |
| POST | `/api/auth/reset-password` | no. Body: `email`, `token`, `newPassword` |
| POST | `/api/auth/change-password` | sí |
| GET | `/api/auth/me` | sí |
| GET | `/api/pacientes/me` | sí |
| PUT | `/api/pacientes/me` | Paciente. Perfil + sexo, alergias, contactoEmergenciaNombre, contactoEmergenciaTelefono |
| PUT | `/api/pacientes/{pacienteId}` | Secretaria / Admin. Mismo body |
| GET | `/api/pacientes?q=` | Secretaria / Admin |
| POST | `/api/pacientes` | Secretaria / Admin |

Password: 8+, mayúscula, minúscula, dígito y símbolo. `sexo`: `M`, `F`, `X` o null.

## Citas

El paciente pide con `especialidadId` + `fechaHoraInicio` (Guatemala, **sin Z**) + `motivoConsulta` (≥ 5). El API asigna el médico primario.

Secretaria/Admin también: `POST /api/citas/para-paciente` con `pacienteId` extra.

`GET /api/citas?pacienteId=` (Secretaria/Admin): citas de ese paciente.

`CitaDto` incluye `pacienteNombre`, `medicoNombre`, `especialidadNombre`.

`GET /api/citas/disponibilidad?especialidadId=&fecha=2026-09-11`

Estados: Solicitada → Programada → Confirmada → En Espera → En Atencion → Atendida. Alternativas: Rechazada, Cancelada, No presentada.

`POST /api/citas/{id}/llegada` acepta **Programada o Confirmada**.

`POST /api/citas/{id}/reprogramar`. La tercera la hace un Administrador. Cancelar con menos de 2 h → `No presentada`.

`GET /api/citas/{id}/historial` trae `descripcion` en español (quién, de/hacia, horas).

## Admin (`/api/admin`, solo Administrador)

Especialidades crear/editar. Médicos crear/editar. Horarios crear/desactivar. Usuarios listar y activar/desactivar (no se desactiva un admin). Parámetros PUT valor. `GET /api/admin/auditoria`.

## Reportes y notificaciones

`GET /api/reportes/citas?desde=&hasta=&medicoId=`

`GET /api/notificaciones/mias` (Paciente) y `GET /api/notificaciones` (staff). Correo SMTP; no hay WhatsApp.

En el VPS: variables `Smtp__UserName` y `Smtp__Password`. Ver `src/ClinicaPro.Api/README.md`.

## Cuentas demo

Tras `POST /api/demo/preparar-agenda`:

- `admin@clinica.com` / `Admin123!`
- `secretaria@clinica.com` / `Secretaria123!`
- `medico@clinica.com` / `Medico123!` (Carlos Hernandez)
- `medico2@clinica.com` / `Medico123!` (Ana Morales)

Medicina General: `20000000-0000-0000-0000-000000000001`. Horario lun–vie 08:00–16:00. Agenda por médico: `GET /api/citas/agenda?medicoId=`.
