using ClinicaPro.Contracts.Catalogs;

namespace ClinicaPro.Application.Catalogs;

public interface ICatalogRepository
{
    Task<IReadOnlyCollection<SpecialtyResponse>> GetActiveSpecialtiesAsync(CancellationToken cancellationToken);
}
