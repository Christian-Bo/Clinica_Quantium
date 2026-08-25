using ClinicaPro.Application.Especialidades;
using ClinicaPro.Infrastructure.Persistence;
using ClinicaPro.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClinicaPro")
            ?? throw new InvalidOperationException(
                "No se encontró ConnectionStrings:ClinicaPro en la configuración de la API.");

        services.AddDbContext<ClinicaProDbContext>(options =>
            options.UseSqlServer(connectionString, sqlServer =>
            {
                sqlServer.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        services.AddScoped<IEspecialidadRepository, EspecialidadRepository>();

        return services;
    }
}
