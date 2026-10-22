# Documentación

Documentación técnica del proyecto Clínica Quantium.

## Índice

| Documento | Qué cubre | Para quién |
|---|---|---|
| `Documento_Arquitectura_ClinicaPro.pdf` | El documento de arquitectura entregado al curso | Todo el equipo |
| `MODELO_DATOS.md` | Las 18 tablas, sus columnas, llaves foráneas, índices, triggers y columnas calculadas | Quien toque la base de datos |
| `API_ENDPOINTS.md` | Las rutas de la API con su rol autorizado, cuerpo de petición, respuesta y errores | Frontend y QA |
| `PLAN_PRUEBAS.md` | Estrategia de pruebas, cobertura actual y el catálogo de casos manuales | QA |
| `DECISIONES_ARQUITECTURA.md` | Registro de decisiones técnicas: el problema, la alternativa descartada y la consecuencia | Quien necesite entender por qué algo está hecho así |
| `MATRIZ_REGLAS.md` | Cruce entre las reglas del documento de arquitectura y su implementación real | Quien revise cumplimiento de requisitos |

## Por dónde empezar

Si es la primera vez que ves el proyecto, el orden que menos cuesta es:

1. `DECISIONES_ARQUITECTURA.md`, ADR-01 a ADR-03, para entender cómo está partido el sistema.
2. `MODELO_DATOS.md`, secciones 1 a 5, para entender la tabla `Citas` y sus reglas.
3. `API_ENDPOINTS.md`, sección 3, para ver cómo se expone todo eso por HTTP.

## Notas de mantenimiento

Estos documentos describen el estado de la rama `develop` en el commit indicado en cada uno.
Al cambiar una regla de negocio, un endpoint o el esquema, conviene actualizar el documento
correspondiente en el mismo *pull request* que el código.

La documentación de la base de datos describe el esquema, pero **la fuente de verdad del
código de los triggers es la propia base**, no el repositorio. Para ver el que está vivo:

```sql
SELECT tr.name, OBJECT_NAME(tr.parent_id) AS Tabla, m.definition
FROM sys.triggers tr
JOIN sys.sql_modules m ON m.object_id = tr.object_id
WHERE tr.parent_id > 0;
```
