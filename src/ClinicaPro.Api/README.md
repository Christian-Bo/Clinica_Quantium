# ClinicaPro.Api

API REST del sistema. Mantener los controladores delgados: reciben la petición, delegan en Application y devuelven la respuesta.
La conexión SQL se consume a través de Infrastructure; no escribir SQL ni usar DbContext directamente desde controladores de negocio.

## Correo (SMTP) — local y VPS

`appsettings.json` ya trae `Host=smtp.gmail.com`. **UserName y Password van vacíos en git.**

En el VPS (IIS, variables de entorno del sistema o del Application Pool), sin `secrets.json`:

```text
Smtp__Host=smtp.gmail.com
Smtp__UserName=tu-correo@gmail.com
Smtp__Password=contraseña-de-aplicacion
Smtp__From=Clínica Quantium <tu-correo@gmail.com>
```

Gmail exige [contraseña de aplicación](https://myaccount.google.com/apppasswords). Si `UserName` queda vacío, el worker escribe archivos en `App_Data/mail`.

**Marca (COR-04).** Los asuntos de correo y Swagger usan el nombre oficial **Clínica Quantium** (`ClinicaMarca`). No hace falta una variable nueva. Si `Smtp__From` todavía dice “Clínica Pro”, la API solo cambia esa marca y **deja el correo igual** (`Clínica Pro <cuenta@gmail.com>` → `Clínica Quantium <cuenta@gmail.com>`). Host, UserName y Password no cambian.

Comprobar a qué SQL pega la API: `GET /api/health/database` → `server` debe ser `ClinicaPro.mssql.somee.com`.
