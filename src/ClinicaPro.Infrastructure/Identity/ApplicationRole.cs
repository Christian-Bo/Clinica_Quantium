using Microsoft.AspNetCore.Identity;

namespace ClinicaPro.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
