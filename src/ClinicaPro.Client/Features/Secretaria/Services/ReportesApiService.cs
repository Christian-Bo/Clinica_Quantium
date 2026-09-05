namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class ReportesApiService(ApiClient api)
{
    private const string FormatoFecha = "yyyy-MM-ddTHH:mm:ss";

    public Task<ReporteCitasDto> ObtenerReporteCitasAsync(
        DateTime desde,
        DateTime hasta,
        Guid? medicoId = null,
        CancellationToken ct = default)
    {
        var url = $"api/reportes/citas?desde={Uri.EscapeDataString(desde.ToString(FormatoFecha))}" +
                  $"&hasta={Uri.EscapeDataString(hasta.ToString(FormatoFecha))}";

        if (medicoId is not null)
        {
            url += $"&medicoId={medicoId}";
        }

        return api.ObtenerRequeridoAsync<ReporteCitasDto>(
            url,
            "No fue posible generar el reporte.",
            ct);
    }
}
