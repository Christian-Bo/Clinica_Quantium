namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class NotificacionesApiService(HttpClient http)
{
    private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss";

    public async Task<IReadOnlyList<NotificacionDto>> ListarAsync(
        string? estado = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        CancellationToken ct = default)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(estado))
        {
            query.Add($"estado={Uri.EscapeDataString(estado)}");
        }

        if (desde is not null)
        {
            query.Add($"desde={Uri.EscapeDataString(desde.Value.ToString(FormatoFecha))}");
        }

        if (hasta is not null)
        {
            query.Add($"hasta={Uri.EscapeDataString(hasta.Value.ToString(FormatoFecha))}");
        }

        var url = "api/notificaciones" + (query.Count > 0 ? "?" + string.Join('&', query) : string.Empty);
        return await http.GetFromJsonAsync<List<NotificacionDto>>(url, ct) ?? [];
    }
}
