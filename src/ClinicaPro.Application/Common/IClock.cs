namespace ClinicaPro.Application.Common;

public interface IClock
{
    DateTime UtcNow { get; }
    DateTime LocalNow { get; }
}
