# Catálogo de referencias del frontend — Clínica Pro

Los códigos `CP-FE-*` solo aparecen cuando el problema es técnico y el usuario no puede resolverlo corrigiendo un campo. Los errores funcionales (correo inválido, sin permiso, duplicado, etc.) se muestran directamente y no necesitan código.

| Código | Significado técnico | Primera revisión |
|---|---|---|
| CP-FE-001 | No se pudo establecer comunicación con la API | URL de API, red, CORS, servicio publicado |
| CP-FE-002 | Timeout al esperar respuesta | latencia, API, base de datos, red |
| CP-FE-010 | Respuesta no compatible con el contrato esperado | DTO/Contracts, versión desplegada de API y Client |
| CP-FE-050 | Error 5xx del servidor o infraestructura | logs de API, trace/correlation id, base de datos |

## Regla de soporte

1. Pedir al usuario únicamente el código de referencia y la operación que intentaba realizar.
2. No solicitar contraseñas, JWT ni datos clínicos por chat/correo.
3. Si el backend devuelve un identificador de correlación en el futuro, mostrarlo junto con `CP-FE-*`.
4. El frontend nunca debe inventar una causa SQL para un HTTP 500; solo indica que el servidor no completó la operación.
