namespace ClinicaPro.Contracts.Especialidades;

public sealed record EspecialidadDto(
    Guid EspecialidadId,
    string Nombre,
    string? Descripcion);
