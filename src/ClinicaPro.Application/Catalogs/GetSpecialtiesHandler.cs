using ClinicaPro.Contracts.Catalogs;

namespace ClinicaPro.Application.Catalogs;

public sealed class GetSpecialtiesHandler
{
    private readonly ICatalogRepository _catalogRepository;

    public GetSpecialtiesHandler(ICatalogRepository catalogRepository)
    {
        _catalogRepository = catalogRepository;
    }

    public Task<IReadOnlyCollection<SpecialtyResponse>> HandleAsync(CancellationToken cancellationToken) =>
        _catalogRepository.GetActiveSpecialtiesAsync(cancellationToken);
}
