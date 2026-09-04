using ClinicaPro.Application.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicaPro.Infrastructure.Auth;

public sealed class MemoryAuthAttemptLimiter(IMemoryCache cache) : IAuthAttemptLimiter
{
    public const int MaxLoginPorIp = 30;
    public const int MaxForgotPorIp = 8;
    public const int MaxForgotPorEmail = 3;
    private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);

    public bool TryAcquireLogin(string ip) => TryIncrement($"login:{Normalizar(ip)}", MaxLoginPorIp);

    public bool TryAcquireForgotPassword(string ip, string email)
        => TryIncrement($"forgot-ip:{Normalizar(ip)}", MaxForgotPorIp)
            && TryIncrement($"forgot-email:{Normalizar(email)}", MaxForgotPorEmail);

    private bool TryIncrement(string clave, int maximo)
    {
        var actual = cache.Get<int?>(clave) ?? 0;
        if (actual >= maximo)
        {
            return false;
        }

        cache.Set(clave, actual + 1, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Ventana
        });
        return true;
    }

    private static string Normalizar(string valor)
        => string.IsNullOrWhiteSpace(valor) ? "unknown" : valor.Trim().ToLowerInvariant();
}
