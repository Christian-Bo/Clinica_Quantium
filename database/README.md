# Base de datos

La base objetivo es SQL Server. El backend usa EF Core y permite iniciar con una base en memoria en ambiente Development.

El script `001_initial_schema.sql` contiene la estructura mínima equivalente para la funcionalidad inicial de solicitudes de cita.

No deben guardarse en GitHub:

- respaldos `.bak`;
- archivos `.mdf` o `.ldf`;
- contraseñas;
- cadenas de producción;
- datos reales de pacientes.
