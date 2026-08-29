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

public sealed record CrearHorarioRequest(
    byte DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly? VigenteDesde,
    DateOnly? VigenteHasta);

public sealed record ActualizarHorarioRequest(
    byte DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin,
    DateOnly? VigenteDesde,
    DateOnly? VigenteHasta,
    bool IsActive);

public sealed record AsignarEspecialidadMedicoRequest(Guid EspecialidadId, bool EsPrimario);

public sealed record ActualizarEspecialidadMedicoRequest(bool EsPrimario, bool IsActive);

public sealed record MedicoEspecialidadAdminDto(
    Guid EspecialidadId,
    string Nombre,
    bool EsPrimario,
    bool IsActive);

public sealed record AdminMedicoDto(
    Guid MedicoId,
    Guid UsuarioId,
    string Email,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string? NumeroColegiado,
    string? Telefono,
    bool IsActive,
    IReadOnlyList<Guid> EspecialidadIds,
    Guid? EspecialidadPrimariaId);

public sealed record AutorizacionReprogramacionDto(
    Guid AutorizacionId,
    Guid CitaId,
    Guid SolicitadaPorUsuarioId,
    Guid? AutorizadaPorUsuarioId,
    string Estado,
    string MotivoSolicitud,
    string? MotivoDecision,
    DateTime CreatedAtUtc,
    DateTime? DecididaAtUtc);

public sealed record ActualizarParametroRequest(string Valor);

public sealed record ActualizarUsuarioRequest(bool IsActive);

public sealed record CrearUsuarioStaffRequest(string Email, string Password, string Rol);

public sealed record ActualizarRolesUsuarioRequest(IReadOnlyList<string> Roles);

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
