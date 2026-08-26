using ClinicaPro.Domain;

namespace ClinicaPro.Application.Citas;

public sealed record ActorResumen(Guid UsuarioId, string Nombre, string Rol);

public interface IActorConsulta
{
    Task<IReadOnlyDictionary<Guid, ActorResumen>> ObtenerPorIdsAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        CancellationToken cancellationToken = default);
}

public sealed record HistorialCitaExplicativo(
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

public static class HistorialCitaNarrativa
{
    public static string Redactar(
        string tipoCambio,
        string actorNombre,
        string actorRol,
        string? estadoAnterior,
        string? estadoNuevo,
        DateTime? fechaHoraInicioAnterior,
        DateTime? fechaHoraInicioNueva,
        DateTime? fechaHoraFinAnterior,
        DateTime? fechaHoraFinNueva,
        string motivo)
    {
        var quien = $"{actorNombre} ({RolVisible(actorRol)})";
        var motivoTexto = string.IsNullOrWhiteSpace(motivo) ? string.Empty : $" Motivo: {motivo.Trim()}.";

        return tipoCambio switch
        {
            "Creacion" =>
                $"{quien} registró la solicitud de cita para el {FormatoRango(fechaHoraInicioNueva, fechaHoraFinNueva)}.",
            "Reprogramacion" =>
                $"{quien} reprogramó la cita del {FormatoRango(fechaHoraInicioAnterior, fechaHoraFinAnterior)} al {FormatoRango(fechaHoraInicioNueva, fechaHoraFinNueva)}.{motivoTexto}",
            "Cancelacion" =>
                $"{quien} canceló la cita.{motivoTexto}",
            _ => estadoAnterior is null
                ? $"{quien} actualizó la cita.{motivoTexto}"
                : $"{quien} cambió el estado de {estadoAnterior} a {estadoNuevo}.{motivoTexto}"
        };
    }

    private static string RolVisible(string rol) => rol switch
    {
        RolNombres.Medico => "Médico",
        RolNombres.Secretaria => "Secretaria",
        RolNombres.Administrador => "Administrador",
        RolNombres.Paciente => "Paciente",
        _ => string.IsNullOrWhiteSpace(rol) ? "Usuario" : rol
    };

    private static string FormatoRango(DateTime? inicio, DateTime? fin)
    {
        if (inicio is null)
        {
            return "una fecha no registrada";
        }

        var textoInicio = inicio.Value.ToString("dd/MM/yyyy HH:mm");
        return fin is null
            ? textoInicio
            : $"{textoInicio} a {fin.Value:HH:mm}";
    }
}
