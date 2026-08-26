using ClinicaPro.Application.Pacientes;
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
    public Task<IReadOnlyList<Notificacion>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return notificaciones.ListarRecientesAsync(100, cancellationToken);
    }
}
