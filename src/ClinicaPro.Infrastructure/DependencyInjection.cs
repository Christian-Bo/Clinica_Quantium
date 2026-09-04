using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Admin;
using ClinicaPro.Application.Auth;
using ClinicaPro.Application.Citas;

using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Infrastructure.Admin;
using ClinicaPro.Infrastructure.Auth;
using ClinicaPro.Infrastructure.Demo;
using ClinicaPro.Infrastructure.Email;
using ClinicaPro.Infrastructure.Identity;
using ClinicaPro.Infrastructure.Persistence;
using ClinicaPro.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Identity;
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
            .AddEntityFrameworkStores<ClinicaProDbContext>()
            .AddDefaultTokenProviders();

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
      
        services.AddScoped<IPacienteRepository, PacienteRepository>();
        services.AddScoped<IMedicoRepository, MedicoRepository>();
        services.AddScoped<IHorarioRepository, HorarioRepository>();
        services.AddScoped<ICitaRepository, CitaRepository>();
        services.AddScoped<IHistorialCitaRepository, HistorialCitaRepository>();
        services.AddScoped<IAutorizacionReprogramacionRepository, AutorizacionReprogramacionRepository>();
        services.AddScoped<IParametroRepository, ParametroRepository>();
        services.AddScoped<INotificacionRepository, NotificacionRepository>();
        services.AddScoped<IActorConsulta, ActorConsulta>();
        services.AddScoped<IAuditoriaWriter, AuditoriaWriter>();
        services.AddScoped<IAdminStaffService, AdminStaffService>();
        services.AddScoped<IPrepararAgendaDemo, PrepararAgendaDemoService>();

        var smtpSection = configuration.GetSection(SmtpOptions.SectionName);
        services.Configure<SmtpOptions>(options =>
        {
            options.Host = smtpSection["Host"] ?? string.Empty;
            options.UserName = smtpSection["UserName"] ?? string.Empty;
            options.Password = smtpSection["Password"] ?? string.Empty;
            options.From = smtpSection["From"] ?? options.From;
            options.PickupDirectory = smtpSection["PickupDirectory"] ?? options.PickupDirectory;
            if (int.TryParse(smtpSection["Port"], out var port))
            {
                options.Port = port;
            }

            if (bool.TryParse(smtpSection["EnableSsl"], out var ssl))
            {
                options.EnableSsl = ssl;
            }
        });
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<NotificationDispatchWorker>();
        services.AddHostedService<AppointmentReminderWorker>();

        return services;
    }
}
