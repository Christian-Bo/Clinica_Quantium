# ClinicaPro.Api

API REST del sistema. Mantener los controladores delgados: reciben la petición, delegan en Application y devuelven la respuesta.
La conexión SQL se consume a través de Infrastructure; no escribir SQL ni usar DbContext directamente desde controladores de negocio.
