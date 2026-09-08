using ClinicaPro.Client.Shared.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;

namespace ClinicaPro.Client.Features.Medico.Services;

public sealed class AgendaMedicoRealtimeService(
    IConfiguration configuration,
    TokenStorageService tokenStorage) : IAsyncDisposable
{
    private HubConnection? connection;
    public event Func<PacienteLlegoDto, Task>? PacienteLlego;

    public bool Conectado => connection?.State == HubConnectionState.Connected;

    public async Task IniciarAsync(CancellationToken ct = default)
    {
        if (connection?.State is HubConnectionState.Connected or HubConnectionState.Connecting or HubConnectionState.Reconnecting)
        {
            return;
        }

        if (connection is not null)
        {
            await connection.DisposeAsync();
        }

        var baseUrl = configuration["ApiBaseUrl"] ?? "https://136-113-48-173.sslip.io/";
        var hubUrl = new Uri(new Uri(baseUrl), "hubs/agenda-medico");
        connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.AccessTokenProvider = async () => (await tokenStorage.ObtenerAsync())?.AccessToken;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<PacienteLlegoDto>("pacienteLlego", async aviso =>
        {
            var handler = PacienteLlego;
            if (handler is not null)
            {
                await handler.Invoke(aviso);
            }
        });

        try
        {
            await connection.StartAsync(ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            // La agenda conserva el refresco periódico como respaldo si el canal en tiempo real no está disponible.
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
        {
            await connection.DisposeAsync();
            connection = null;
        }
    }
}
