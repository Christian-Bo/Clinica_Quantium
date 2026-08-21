# Clínica Pro - Base inicial

Base de desarrollo del **Módulo de Gestión de Citas**. Mantiene la arquitectura definida para el proyecto: Blazor WebAssembly + ASP.NET Core Web API + Clean Architecture + EF Core + SQL Server.

## Estado de esta base

Esta entrega es intencionalmente mínima. No implementa todavía los casos de uso de citas, Identity, notificaciones ni auditoría en código. Deja preparada la solución para que el equipo empiece a desarrollar sin arrastrar clases de demostración o implementaciones parciales.

La base SQL de `ClinicaPro` ya debe existir en Somee. La API **no crea, borra ni migra la base al iniciar**.

## Proyectos

- `ClinicaPro.Domain`: entidades y reglas de negocio. No depende de tecnología.
- `ClinicaPro.Application`: casos de uso e interfaces. Solo depende de Domain.
- `ClinicaPro.Infrastructure`: EF Core, SQL Server y futuras implementaciones técnicas. Depende de Application y Domain.
- `ClinicaPro.Contracts`: DTOs compartidos entre API y Client.
- `ClinicaPro.Api`: endpoints, middleware, CORS y composición de dependencias.
- `ClinicaPro.Client`: Blazor WebAssembly. Solo consume la API.

## Dependencias permitidas

```text
Domain            -> ninguna
Application       -> Domain
Infrastructure    -> Application + Domain
Contracts         -> ninguna
Api               -> Application + Infrastructure + Contracts
Client            -> Contracts
```

No agregar referencias en sentido contrario.

## Requisitos

- .NET 8 SDK
- Node.js LTS (solo para compilar Tailwind)
- Acceso a Internet para restaurar paquetes NuGet/NPM

## Primera ejecución

Desde la raíz:

```powershell
dotnet restore ClinicaPro.sln
dotnet build ClinicaPro.sln
```

Frontend:

```powershell
cd src/ClinicaPro.Client
npm install
npm run build
cd ../..
```

Pruebas:

```powershell
dotnet test ClinicaPro.sln
```

API:

```powershell
dotnet run --project src/ClinicaPro.Api
```

Client (otra terminal):

```powershell
dotnet run --project src/ClinicaPro.Client
```

## Comprobaciones rápidas

Con la API iniciada:

- `GET /api/health` comprueba que la API está levantada.
- `GET /api/health/database` comprueba que EF Core puede conectarse a la base remota de Somee.
- Swagger está habilitado en ambiente Development.

## Base de datos compartida

La cadena de conexión de Somee se dejó dentro de `src/ClinicaPro.Api/appsettings.json` **porque el equipo solicitó una conexión compartida inmediata**.

Esto NO debe conservarse así para un repositorio público ni para producción. Antes de publicar el proyecto o pasar a un ambiente real, mover la cadena a User Secrets, variables de entorno o secretos del hosting y rotar la contraseña.

No sustituir el hostname de Somee por una IP fija en el repositorio. Si una red local no resuelve el hostname, corregir DNS en esa computadora/red.

## Base SQL

`database/ClinicaPro_BD_Somee.sql` contiene el esquema compartido.
`database/ClinicaPro_Validacion_Somee.sql` permite comprobar la instalación.

Una vez que se empiecen a mapear las entidades reales en EF Core, los cambios de esquema deberán coordinarse y evolucionar mediante migraciones revisadas. Nadie debe modificar tablas compartidas manualmente sin avisar al equipo.

## Git

Flujo mínimo:

```text
main       -> entrega estable
develop    -> integración
feature/*  -> funcionalidades
fix/*      -> correcciones
docs/*     -> documentación
```

Cada funcionalidad debe entrar por Pull Request hacia `develop`. No trabajar directamente en `main`.
