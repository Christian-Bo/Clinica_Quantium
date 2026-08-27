using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Notificaciones;

public sealed class ListarNotificacionesPacienteService(
    IPacienteRepository pacientes,
    INotificacionRepository notificaciones)
{
    public async Task<IReadOnlyList<Notificacion>> ExecuteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        return await notificaciones.ListarPorPacienteAsync(paciente.Id, cancellationToken);
    }
}

public sealed class ListarNotificacionesStaffService(INotificacionRepository notificaciones)
{
    private static readonly HashSet<string> EstadosValidos =
    [
        NotificacionEstados.Pendiente,
        NotificacionEstados.Procesando,
        NotificacionEstados.Enviada,
        NotificacionEstados.Fallida
    ];

    public Task<IReadOnlyList<Notificacion>> ExecuteAsync(
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken cancellationToken = default)
    {
        var estadoFiltro = string.IsNullOrWhiteSpace(estado) ? null : estado.Trim();
        if (estadoFiltro is not null && !EstadosValidos.Contains(estadoFiltro))
        {
            throw new DomainException("El estado de notificación no es válido.");
        }

        var desdeUtc = desde is null ? (DateTime?)null : HoraClinica.AUtc(InicioDelDia(desde.Value));
        var hastaUtcExclusivo = hasta is null ? (DateTime?)null : HoraClinica.AUtc(FinExclusivo(hasta.Value));
        if (desdeUtc is not null && hastaUtcExclusivo is not null && hastaUtcExclusivo <= desdeUtc)
        {
            throw new DomainException("El rango de fechas de notificaciones es inválido.");
        }

        return notificaciones.ListarStaffAsync(
            estadoFiltro,
            desdeUtc,
            hastaUtcExclusivo,
            cantidadMaxima: 100,
            cancellationToken);
    }

    private static DateTime InicioDelDia(DateTime valor)
    {
        return valor.TimeOfDay == TimeSpan.Zero
            ? valor.Date
            : valor;
    }

    private static DateTime FinExclusivo(DateTime valor)
    {
        return valor.TimeOfDay == TimeSpan.Zero
            ? valor.Date.AddDays(1)
            : valor.AddTicks(1);
    }
}
