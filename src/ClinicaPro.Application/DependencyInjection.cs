using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Registrar aquí los casos de uso cuando se implementen.
        return services;
    }
}
