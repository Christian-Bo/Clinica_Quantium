namespace ClinicaPro.Contracts.Especialidades;

public sealed record EspecialidadDto(
    Guid EspecialidadId,
    string Nombre,
    string? Descripcion);

public sealed record CrearEspecialidadRequest(string Nombre, string? Descripcion);

public sealed record ActualizarEspecialidadRequest(string Nombre, string? Descripcion, bool IsActive = true);

public sealed record MedicoEspecialidadAdminDto(
    Guid EspecialidadId,
    string Nombre,
    bool EsPrimario,
    bool IsActive);

public sealed record AsignarEspecialidadMedicoRequest(Guid EspecialidadId, bool EsPrimario);

public sealed record ActualizarEspecialidadMedicoRequest(bool EsPrimario, bool IsActive);
