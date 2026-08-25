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

public sealed record CitaDto(
    Guid CitaId,
    Guid PacienteId,
    Guid MedicoId,
    Guid EspecialidadId,
    DateTime FechaHoraInicio,
    DateTime FechaHoraFin,
    string MotivoConsulta,
    string Estado);

public sealed record MotivoCitaRequest(string Motivo);

public sealed record HistorialCitaDto(
    long HistorialCitaId,
    Guid CitaId,
    Guid UsuarioId,
    string TipoCambio,
    string? EstadoAnterior,
    string? EstadoNuevo,
    string Motivo,
    DateTime FechaCambioUtc);

public sealed record ParametroDto(
    string Clave,
    string Valor,
    string TipoDato,
    string? Descripcion);
