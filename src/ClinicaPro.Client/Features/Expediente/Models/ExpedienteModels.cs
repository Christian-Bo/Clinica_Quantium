namespace ClinicaPro.Client.Features.Expediente.Models;

public sealed record PacienteContextoExpedienteDto(
    Guid PacienteId,
    string NombreCompleto,
    string? Sexo,
    string? Alergias,
    DateOnly? FechaNacimiento,
    string? Telefono);

public sealed record PreconsultaExpedienteDto(
    Guid PreconsultaId,
    Guid CitaId,
    short PresionSistolicaMmHg,
    short PresionDiastolicaMmHg,
    decimal TemperaturaCelsius,
    decimal OxigenoSangrePorcentaje,
    string? Observacion,
    Guid RegistradaPorUsuarioId,
    DateTime FechaRegistroUtc);

public sealed record IrisExpedienteDto(
    Guid ImagenIrisId,
    Guid CitaId,
    Guid TomadaPorUsuarioId,
    string Lateralidad,
    string NombreArchivo,
    string TipoContenido,
    long? TamanoBytes,
    string? Observacion,
    DateTime FechaCapturaUtc,
    string UrlArchivo);

public sealed record ExpedienteCitaClientDto(
    CitaDto Cita,
    PacienteContextoExpedienteDto Paciente,
    PreconsultaExpedienteDto? Preconsulta,
    IReadOnlyList<IrisExpedienteDto> ImagenesIris);

public sealed record ExpedienteVisitaResumenClientDto(
    Guid CitaId,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    string Estado,
    Guid MedicoId,
    string MedicoNombre,
    string MotivoConsulta,
    bool TienePreconsulta,
    int CantidadIris,
    short? PresionSistolicaMmHg,
    short? PresionDiastolicaMmHg,
    decimal? TemperaturaCelsius,
    decimal? OxigenoSangrePorcentaje);

public sealed record ExpedientePacienteClientDto(
    PacienteContextoExpedienteDto Paciente,
    IReadOnlyList<ExpedienteVisitaResumenClientDto> Visitas);

public sealed record ImagenIrisClientDto(
    Guid ImagenIrisId,
    Guid CitaId,
    Guid TomadaPorUsuarioId,
    string Lateralidad,
    string NombreArchivo,
    string TipoContenido,
    long? TamanoBytes,
    string? Observacion,
    DateTime FechaCapturaUtc);
