using ClinicaPro.Application.Admin;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Citas;

using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicaPro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
    
        services.AddScoped<ListarMedicosActivosService>();
        services.AddScoped<SolicitarCitaService>();
        services.AddScoped<OperarCitaService>();
        services.AddScoped<ListarCitasPacienteService>();
        services.AddScoped<ListarCitasMedicoService>();
        services.AddScoped<ListarCitasPorPacienteStaffService>();
        services.AddScoped<ListarCitasPendientesService>();
        services.AddScoped<ListarAgendaService>();
        services.AddScoped<ListarDisponibilidadService>();
        services.AddScoped<ResolverNombresCitaService>();
        services.AddScoped<ReprogramarCitaService>();
        services.AddScoped<CancelarCitaService>();
        services.AddScoped<ListarHistorialCitaService>();
        services.AddScoped<HistorialMedicoPacienteService>();
        services.AddScoped<IAvisoTiempoRealAgenda, AvisoTiempoRealAgendaNulo>();
        services.AddScoped<AjustarRecordatorioCitaService>();
        services.AddScoped<SolicitarAutorizacionReprogramacionService>();
        services.AddScoped<ListarAutorizacionesReprogramacionService>();
        services.AddScoped<ResolverAutorizacionReprogramacionService>();
    
        services.AddScoped<BuscarPacientesService>();
        services.AddScoped<ListarReporteCitasService>();
        services.AddScoped<EncolarNotificacionCitaService>();
        services.AddScoped<EncolarRecordatoriosCitaService>();
        services.AddScoped<ListarNotificacionesPacienteService>();
        services.AddScoped<ListarNotificacionesStaffService>();
        services.AddScoped<ActualizarPerfilPacienteService>();
        
        services.AddScoped<AdministrarHorariosService>();
        services.AddScoped<AdministrarParametrosService>();
        return services;
    }
}
