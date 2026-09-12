namespace ClinicaPro.Client.Features.Secretaria.Models;

/// <summary>
/// Modelo de respuesta usado por el frontend para la preconsulta.
/// Mantiene la misma forma JSON que ClinicaPro.Contracts.Agenda.PreconsultaDto
/// disponible en el backend actualizado.
/// </summary>
public sealed record PreconsultaClientDto(
    Guid PreconsultaId,
    Guid CitaId,
    short PresionSistolicaMmHg,
    short PresionDiastolicaMmHg,
    decimal TemperaturaCelsius,
    decimal OxigenoSangrePorcentaje,
    string? Observacion,
    Guid RegistradaPorUsuarioId,
    DateTime FechaRegistroUtc);

/// <summary>
/// Payload enviado al endpoint PUT api/citas/{citaId}/preconsulta.
/// Los nombres de las propiedades coinciden con RegistrarPreconsultaRequest
/// del backend para mantener compatibilidad de serialización JSON.
/// </summary>
public sealed record RegistrarPreconsultaClientRequest(
    short PresionSistolicaMmHg,
    short PresionDiastolicaMmHg,
    decimal TemperaturaCelsius,
    decimal OxigenoSangrePorcentaje,
    string? Observacion);
