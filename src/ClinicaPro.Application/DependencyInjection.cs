using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;
using ClinicaPro.Application.Especialidades;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ListarEspecialidadesActivasService>();
        services.AddScoped<ListarMedicosActivosService>();
        services.AddScoped<SolicitarCitaService>();
        services.AddScoped<OperarCitaService>();
        services.AddScoped<ListarCitasPacienteService>();
        services.AddScoped<ListarCitasMedicoService>();
        services.AddScoped<ListarCitasPendientesService>();
        services.AddScoped<ListarAgendaService>();
        return services;
    }
}
