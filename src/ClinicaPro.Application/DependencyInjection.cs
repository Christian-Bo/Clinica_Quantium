using ClinicaPro.Application.Appointments;
using ClinicaPro.Application.Catalogs;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<RequestAppointmentHandler>();
        services.AddScoped<GetSpecialtiesHandler>();
        return services;
    }
}
