# ClinicaPro.Infrastructure

Aquí se implementarán EF Core, SQL Server, Identity, repositorios, auditoría y notificaciones.

La conexión se registra en `DependencyInjection.cs` y utiliza `ConnectionStrings:ClinicaPro`.
El proyecto NO ejecuta `EnsureCreated`, `Migrate` ni seeds automáticamente al iniciar: la base remota compartida no debe alterarse por arrancar la API.
