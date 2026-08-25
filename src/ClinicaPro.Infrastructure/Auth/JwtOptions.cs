namespace ClinicaPro.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ClinicaPro.Api";
    public string Audience { get; set; } = "ClinicaPro.Client";
    public string Key { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 120;
}
