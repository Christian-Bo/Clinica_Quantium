using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Auth;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Infrastructure.Auth;
using ClinicaPro.Infrastructure.Demo;
using ClinicaPro.Infrastructure.Identity;
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

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ClinicaProDbContext>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtSection["Issuer"] ?? options.Issuer;
            options.Audience = jwtSection["Audience"] ?? options.Audience;
            options.Key = jwtSection["Key"] ?? string.Empty;
            if (int.TryParse(jwtSection["ExpirationMinutes"], out var minutes))
            {
                options.ExpirationMinutes = minutes;
            }
        });

        services.AddSingleton<JwtTokenGenerator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<IEspecialidadRepository, EspecialidadRepository>();
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IMedicoRepository, MedicoRepository>();
        services.AddScoped<IHorarioRepository, HorarioRepository>();
        services.AddScoped<ICitaRepository, CitaRepository>();
        services.AddScoped<IHistorialCitaRepository, HistorialCitaRepository>();
        services.AddScoped<IParametroRepository, ParametroRepository>();
        services.AddScoped<IPrepararAgendaDemo, PrepararAgendaDemoService>();

        return services;
    }
}
