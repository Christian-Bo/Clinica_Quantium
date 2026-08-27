namespace ClinicaPro.Contracts.Agenda;

public sealed record MedicoDto(
    Guid MedicoId,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string? NumeroColegiado,
    string? Telefono,
    IReadOnlyList<Guid> EspecialidadIds,
    Guid? EspecialidadPrimariaId);

public sealed record HorarioDto(
    Guid HorarioId,
    Guid MedicoId,
    byte DiaSemana,
    TimeOnly HoraInicio,
    TimeOnly HoraFin);

public sealed record SolicitarCitaRequest(
    Guid EspecialidadId,
    DateTime FechaHoraInicio,
    string MotivoConsulta);

public sealed record SolicitarCitaParaPacienteRequest(
    Guid PacienteId,
    Guid EspecialidadId,
    DateTime FechaHoraInicio,
    string MotivoConsulta);

public sealed record CitaDto(
    Guid CitaId,
    Guid PacienteId,
    string PacienteNombre,
    Guid MedicoId,
    string MedicoNombre,
    Guid EspecialidadId,
    string EspecialidadNombre,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    string MotivoConsulta,
    string Estado,
    byte NumeroReprogramaciones);

public sealed record MotivoCitaRequest(string Motivo);

public sealed record ReprogramarCitaRequest(DateTime FechaHoraInicio, string? Motivo);

public sealed record SlotDisponibleDto(
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    Guid MedicoId,
    string MedicoNombre);

public sealed record HistorialCitaDto(
    long HistorialCitaId,
    Guid CitaId,
    Guid UsuarioId,
    string ActorNombre,
    string ActorRol,
    string TipoCambio,
    string? EstadoAnterior,
    string? EstadoNuevo,
    DateTime? FechaHoraInicioAnterior,
    DateTime? FechaHoraInicioNueva,
    DateTime? FechaHoraFinAnterior,
    DateTime? FechaHoraFinNueva,
    string Motivo,
    DateTime FechaCambioUtc,
    DateTime FechaCambioLocal,
    string Descripcion);

public sealed record ParametroDto(
    string Clave,
    string Valor,
    string TipoDato,
    string? Descripcion);
