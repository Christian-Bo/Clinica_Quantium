using ClinicaPro.Application.Catalogs;
using ClinicaPro.Contracts.Catalogs;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Repositories;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly ClinicaProDbContext _dbContext;

    public CatalogRepository(ClinicaProDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<SpecialtyResponse>> GetActiveSpecialtiesAsync(
        CancellationToken cancellationToken) =>
        await _dbContext.Specialties
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new SpecialtyResponse(x.Id, x.Name))
            .ToListAsync(cancellationToken);
}
