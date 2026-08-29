using ClinicaPro.Api.Hubs;
using ClinicaPro.Application.Citas;
using ClinicaPro.Contracts.Agenda;
using Microsoft.AspNetCore.SignalR;

namespace ClinicaPro.Api.Realtime;

internal sealed class SignalRAvisoTiempoRealAgenda(IHubContext<AgendaMedicoHub> hub) : IAvisoTiempoRealAgenda
{
    public async Task PacienteLlegoAsync(
        Guid medicoUsuarioId,
        PacienteLlegoAviso aviso,
        CancellationToken cancellationToken = default)
    {
        var dto = new PacienteLlegoDto(
            aviso.CitaId,
            aviso.PacienteId,
            aviso.PacienteNombre,
            $"Paciente {aviso.PacienteNombre} ha llegado",
            aviso.FechaHoraInicio);

        await hub.Clients
            .Group(AgendaMedicoHub.GrupoDeUsuario(medicoUsuarioId))
            .SendAsync(AgendaMedicoHub.EventoPacienteLlego, dto, cancellationToken);
    }
}
