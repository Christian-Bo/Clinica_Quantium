# Base de datos

Scripts del esquema de SQL Server para la base compartida en Somee.

**La API no ejecuta ninguno de estos scripts al iniciar.** Se aplican manualmente, en orden,
desde el panel de Somee o desde DBeaver conectado a la base.

## Orden de aplicación

Los scripts son acumulativos y ya están aplicados sobre la base compartida.
**El primero no debe volver a ejecutarse sobre una base con datos.**

| # | Archivo | Qué hace |
|---|---|---|
| 1 | `ClinicaPro_BD_Somee.sql` | Esquema inicial completo: tablas, índices, los dos triggers y las semillas de roles, especialidades y el usuario administrador |
| 2 | `ClinicaPro_PacienteMostrador.sql` | Agrega a `Pacientes` las columnas `Sexo`, `Alergias` y contacto de emergencia |
| 3 | `ClinicaPro_DpiUnico.sql` | Crea el índice único filtrado `UQ_Pacientes_Documento` |
| 4 | `ClinicaPro_LlegadaProgramada.sql` | Reescribe `TR_Citas_Validaciones` para permitir la transición `Programada` → `En Espera` |
| 5 | `ClinicaPro_CitasIntegridad.sql` | **El vigente.** Reescribe el trigger sin `Especialidades` y agrega la regla de solape del mismo paciente (error 51006) |

## Scripts no aplicados

| Archivo | Por qué |
|---|---|
| `ClinicaPro_AutorizacionReprogramacion.sql` | Opcional. Crea la tabla `AutorizacionesReprogramacion`, que el API **no necesita**: la cola de autorización se guarda en `HistorialCitas` con `TipoCambio = 'Autorizacion'`. No ejecutar sin coordinar con infraestructura |
| `ClinicaPro_Validacion_Somee.sql` | Validaciones estructurales posteriores a la carga. Es de solo lectura, se puede correr cuando se quiera verificar el esquema |

## Lo que falta versionar

Las tablas `Preconsultas` e `ImagenesIris` se crearon directamente en Somee y **no tienen
script en este repositorio**. Conviene generarlo a partir del esquema vivo antes de la
entrega final. Su estructura está documentada en `docs/MODELO_DATOS.md`, sección 10.

## La fuente de verdad de los triggers

Los archivos 1, 4 y 5 contienen tres versiones sucesivas del mismo trigger. Para saber cuál
está vivo en la base, no hay que leer el repositorio sino consultarla:

```sql
SELECT tr.name AS Trigger_, OBJECT_NAME(tr.parent_id) AS Tabla, m.definition AS Codigo
FROM sys.triggers tr
JOIN sys.sql_modules m ON m.object_id = tr.object_id
WHERE tr.parent_id > 0;
```

## Documentación relacionada

El esquema completo —las 18 tablas, sus llaves foráneas, los índices filtrados, las columnas
calculadas, los triggers y sus errores 51000 a 51006— está documentado en
`docs/MODELO_DATOS.md`.
