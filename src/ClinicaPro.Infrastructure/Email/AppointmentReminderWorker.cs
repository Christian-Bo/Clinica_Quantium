using ClinicaPro.Application.Notificaciones;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicaPro.Infrastructure.Email;

public sealed class AppointmentReminderWorker(
    IServiceScopeFactory scopes,
    ILogger<AppointmentReminderWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker de recordatorios de cita iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var encolar = scope.ServiceProvider.GetRequiredService<EncolarRecordatoriosCitaService>();
                var cantidad = await encolar.ExecuteAsync(stoppingToken);
                if (cantidad > 0)
                {
                    logger.LogInformation("Se encolaron {Cantidad} recordatorios de cita.", cantidad);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error al encolar recordatorios de cita.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
