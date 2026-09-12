namespace ClinicaPro.Client.Shared;

/// <summary>
/// Fuente única del nombre visible de la clínica en el frontend.
/// El valor se lee de wwwroot/appsettings.json para evitar textos de marca
/// repetidos en componentes, reportes y títulos.
/// </summary>
public sealed class BrandingService(IConfiguration configuration)
{
    public string ClinicName { get; } =
        string.IsNullOrWhiteSpace(configuration["Branding:ClinicName"])
            ? "Clínica Quantium"
            : configuration["Branding:ClinicName"]!.Trim();
}
