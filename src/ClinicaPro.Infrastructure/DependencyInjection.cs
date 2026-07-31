using ClinicaPro.Application.Appointments;
using ClinicaPro.Application.Catalogs;
using ClinicaPro.Application.Common;
using ClinicaPro.Infrastructure.Persistence;
using ClinicaPro.Infrastructure.Repositories;
using ClinicaPro.Infrastructure.Time;
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
        var provider = configuration["DatabaseProvider"] ?? "InMemory";

        services.AddDbContext<ClinicaProDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString("ClinicaProDb")
                    ?? throw new InvalidOperationException("No se encontró la cadena ClinicaProDb.");
                options.UseSqlServer(connectionString);
            }
            else
            {
                options.UseInMemoryDatabase("ClinicaProDevelopment");
            }
        });

        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<ISchedulingRepository, SchedulingRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }

    public static async Task InitialiseDatabaseAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ClinicaProDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        await ClinicaProSeed.SeedAsync(dbContext);
    }
}
