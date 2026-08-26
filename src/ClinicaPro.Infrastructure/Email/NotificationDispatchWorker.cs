using ClinicaPro.Application;
using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicaPro.Infrastructure.Email;

public sealed class NotificationDispatchWorker(
    IServiceScopeFactory scopes,
    ILogger<NotificationDispatchWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker de correos de citas iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcesarLoteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Error al despachar notificaciones por correo.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(8), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task ProcesarLoteAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopes.CreateAsyncScope();
        var notificaciones = scope.ServiceProvider.GetRequiredService<INotificacionRepository>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailSender>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var pendientes = await notificaciones.ListarPendientesAsync(10, cancellationToken);
        foreach (var aviso in pendientes)
        {
            aviso.MarcarProcesando();
            await unitOfWork.SaveChangesAsync(cancellationToken);

            var resultado = await email.SendAsync(
                aviso.Destinatario,
                aviso.Asunto ?? "Clínica Pro",
                aviso.Contenido,
                cancellationToken);

            if (resultado.Succeeded)
            {
                aviso.MarcarEnviada();
            }
            else
            {
                aviso.MarcarIntentoFallido(resultado.Response ?? "No se pudo enviar el correo.");
            }

            await notificaciones.AgregarIntentoAsync(
                IntentoNotificacion.Registrar(
                    aviso.Id,
                    resultado.Succeeded,
                    resultado.ProviderCode,
                    resultado.Response),
                cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Notificación {Id} ({Tipo}) → {Estado}",
                aviso.Id,
                aviso.Tipo,
                aviso.Estado);
        }
    }
}
