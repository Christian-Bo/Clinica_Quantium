using ClinicaPro.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ClinicaPro.Api.Hubs;

[Authorize(Roles = ClinicaPro.Domain.RolNombres.Medico)]
public sealed class AgendaMedicoHub : Hub
{
    public const string Ruta = "/hubs/agenda-medico";
    public const string EventoPacienteLlego = "pacienteLlego";

    public static string GrupoDeUsuario(Guid usuarioId) => $"usuario:{usuarioId:D}";

    public override async Task OnConnectedAsync()
    {
        var usuarioId = Context.User?.ObtenerUsuarioId();
        if (usuarioId is null)
        {
            Context.Abort();
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, GrupoDeUsuario(usuarioId.Value));
        await base.OnConnectedAsync();
    }
}
