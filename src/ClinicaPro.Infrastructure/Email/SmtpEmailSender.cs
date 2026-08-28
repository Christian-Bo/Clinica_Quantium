using System.Text;
using ClinicaPro.Application.Notificaciones;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ClinicaPro.Infrastructure.Email;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "Clínica Pro <noreply@clinica.local>";
    public bool EnableSsl { get; set; } = true;
    public string PickupDirectory { get; set; } = "App_Data/mail";
}

public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    IHostEnvironment environment,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task<EmailSendResult> SendAsync(
        string destinatario,
        string asunto,
        string contenido,
        CancellationToken cancellationToken = default)
    {
        var smtp = options.Value;

        if (string.IsNullOrWhiteSpace(smtp.Host))
        {
            return await GuardarEnCarpetaAsync(destinatario, asunto, contenido, smtp, cancellationToken);
        }

        try
        {
            var message = CrearMensaje(smtp.From, destinatario, asunto, contenido);
            using var client = new SmtpClient();
            var socket = smtp.EnableSsl
                ? (smtp.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls)
                : SecureSocketOptions.None;

            await client.ConnectAsync(smtp.Host, smtp.Port, socket, cancellationToken);

            if (!string.IsNullOrWhiteSpace(smtp.UserName))
            {
                await client.AuthenticateAsync(smtp.UserName, smtp.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            logger.LogInformation("Correo SMTP enviado a {Destinatario} vía {Host}.", destinatario, smtp.Host);
            return new EmailSendResult(true, "smtp", smtp.Host);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "SMTP no pudo enviar el correo a {Destinatario}.", destinatario);
            return new EmailSendResult(false, "smtp", exception.Message);
        }
    }

    private async Task<EmailSendResult> GuardarEnCarpetaAsync(
        string destinatario,
        string asunto,
        string contenido,
        SmtpOptions smtp,
        CancellationToken cancellationToken)
    {
        var carpeta = Path.IsPathRooted(smtp.PickupDirectory)
            ? smtp.PickupDirectory
            : Path.Combine(environment.ContentRootPath, smtp.PickupDirectory);

        Directory.CreateDirectory(carpeta);

        var archivo = Path.Combine(
            carpeta,
            $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}.eml.txt");

        var texto = new StringBuilder()
            .AppendLine($"From: {smtp.From}")
            .AppendLine($"To: {destinatario}")
            .AppendLine($"Subject: {asunto}")
            .AppendLine($"Date: {DateTimeOffset.Now:r}")
            .AppendLine()
            .AppendLine(contenido)
            .ToString();

        await File.WriteAllTextAsync(archivo, texto, Encoding.UTF8, cancellationToken);
        logger.LogInformation("Smtp:Host vacío. Correo de desarrollo escrito en {Archivo}", archivo);
        return new EmailSendResult(true, "file", archivo);
    }

    private static MimeMessage CrearMensaje(string from, string to, string asunto, string contenido)
    {
        var message = new MimeMessage();
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(to));
        message.Subject = asunto;
        message.Body = new TextPart("plain")
        {
            Text = contenido
        };
        return message;
    }
}
