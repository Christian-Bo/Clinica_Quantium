using Microsoft.AspNetCore.Identity;

namespace ClinicaPro.Infrastructure.Identity;

public sealed class ApplicationUserRole : IdentityUserRole<Guid>
{
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
