namespace ClinicaPro.Contracts.Admin;

public sealed record CrearEspecialidadRequest(string Nombre, string? Descripcion);

public sealed record ActualizarEspecialidadRequest(string Nombre, string? Descripcion, bool IsActive);

public sealed record CrearMedicoRequest(
    string Email,
    string Password,
    string Nombres,
    string Apellidos,
    string? NumeroColegiado,
    string? Telefono,
    Guid EspecialidadId,
    bool EsPrimario);

public sealed record ActualizarMedicoRequest(
    string Nombres,
    string Apellidos,
    string? NumeroColegiado,
    string? Telefono,
    bool IsActive);

public sealed record CrearHorarioRequest(byte DiaSemana, TimeOnly HoraInicio, TimeOnly HoraFin);

public sealed record ActualizarParametroRequest(string Valor);

public sealed record ActualizarUsuarioRequest(bool IsActive);

public sealed record UsuarioAdminDto(
    Guid UsuarioId,
    string Email,
    bool IsActive,
    IReadOnlyList<string> Roles);

public sealed record AuditoriaDto(
    long AuditoriaId,
    Guid? UsuarioId,
    string Accion,
    string Entidad,
    string? EntidadId,
    string? Detalle,
    DateTime FechaUtc);
