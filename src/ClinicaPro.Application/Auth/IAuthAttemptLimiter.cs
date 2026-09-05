namespace ClinicaPro.Application.Auth;

public interface IAuthAttemptLimiter
{
    bool TryAcquireLogin(string ip);
    bool TryAcquireForgotPassword(string ip, string email);
}
