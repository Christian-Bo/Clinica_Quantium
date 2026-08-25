using ClinicaPro.Application.Especialidades;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListarEspecialidadesActivasService>();
        return services;
    }
}
