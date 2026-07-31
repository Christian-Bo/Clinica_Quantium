# Clínica Pro

Repositorio base del sistema web de gestión de citas para la Clínica Naturista Quantium.

## Tecnologías

- Frontend: Blazor WebAssembly sobre .NET 8.
- Estilos: Tailwind CSS.
- Interoperabilidad del navegador: TypeScript.
- Backend: ASP.NET Core Web API sobre .NET 8.
- Arquitectura: monolito modular, Clean Architecture, capas y MVC adaptado.
- Persistencia: Entity Framework Core 8.
- Base de datos objetivo: SQL Server.
- Pruebas: xUnit; estructura preparada para integración y recorridos End-to-End.
- Integración continua: GitHub Actions.

## Estructura

```text
ClinicaPro/
├── src/
│   ├── ClinicaPro.Client/          # Frontend Blazor, Tailwind y TypeScript
│   ├── ClinicaPro.Api/             # Backend ASP.NET Core Web API
│   ├── ClinicaPro.Application/     # Casos de uso e interfaces
│   ├── ClinicaPro.Domain/          # Entidades, estados y reglas de negocio
│   ├── ClinicaPro.Infrastructure/  # EF Core, SQL Server y repositorios
│   └── ClinicaPro.Contracts/       # Contratos compartidos de la API
├── tests/
│   ├── ClinicaPro.UnitTests/
│   ├── ClinicaPro.IntegrationTests/
│   └── ClinicaPro.EndToEndTests/
├── database/                       # Script inicial y notas de base de datos
├── deploy/                         # Instrucciones de publicación en IIS
└── .github/workflows/              # Compilación y pruebas automáticas
```

## Inicio rápido

### 1. Restaurar y compilar .NET

```powershell
dotnet restore
dotnet build
```

### 2. Compilar Tailwind CSS y TypeScript

```powershell
cd src/ClinicaPro.Client
npm install
npm run build
cd ../..
```

### 3. Ejecutar el backend

```powershell
dotnet run --project src/ClinicaPro.Api
```

Swagger queda disponible en:

```text
https://localhost:7042/swagger
```

### 4. Ejecutar el frontend en otra terminal

```powershell
dotnet run --project src/ClinicaPro.Client
```

El frontend queda disponible en:

```text
https://localhost:7142
```

## Base de datos

Para que el proyecto pueda abrirse y demostrarse sin instalar SQL Server inmediatamente, el ambiente `Development` usa una base en memoria. Para cambiar a SQL Server:

1. Abrir `src/ClinicaPro.Api/appsettings.Development.json`.
2. Cambiar `DatabaseProvider` de `InMemory` a `SqlServer`.
3. Revisar la cadena `ClinicaProDb`.
4. Ejecutar el script `database/001_initial_schema.sql` o crear migraciones de EF Core.

## Funcionalidad incluida en esta base

- Consulta de especialidades activas.
- Solicitud de cita desde el frontend.
- Asignación del médico primario de la especialidad.
- Validación de fecha futura.
- Validación del horario configurado del médico.
- Prevención de solapamiento con solicitudes o citas activas.
- Registro de la cita en estado `Solicitada`.
- Endpoint de salud del backend.

Esta es una base técnica inicial. La autenticación completa, confirmación administrativa, notificaciones, reportes y auditoría se incorporan por funcionalidades mediante ramas y Pull Requests.

## Flujo de trabajo Git

- `main`: versión estable.
- `develop`: integración del equipo.
- `feature/*`: funcionalidades nuevas.
- `fix/*`: correcciones.
- No guardar contraseñas, cadenas reales, respaldos ni datos de pacientes en el repositorio.
