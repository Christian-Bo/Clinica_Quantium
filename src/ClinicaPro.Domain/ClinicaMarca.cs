using System.Net;
using System.Text;

namespace ClinicaPro.Domain;

/// <summary>
/// Nombre oficial de la clínica. Correos, Swagger y el remitente SMTP
/// deben usar esta constante en lugar de literales.
/// </summary>
public static class ClinicaMarca
{
    public const string Nombre = "Clínica Quantium";

    public const string RemitentePorDefecto = Nombre + " <noreply@clinica.local>";

    public static string Asunto(string detalle) => $"{Nombre} — {detalle}";

    public static string Remitente(string? configurado)
    {
        if (string.IsNullOrWhiteSpace(configurado))
        {
            return RemitentePorDefecto;
        }

        return configurado.Trim().Replace("Clínica Pro", Nombre, StringComparison.Ordinal);
    }

    public static string CuerpoHtml(string? textoPlano)
    {
        var seguro = WebUtility.HtmlEncode(textoPlano ?? string.Empty)
            .Replace("\r\n", "\n", StringComparison.Ordinal);
        var bloques = seguro.Split("\n\n", StringSplitOptions.None);
        var cuerpo = new StringBuilder();
        foreach (var bloque in bloques)
        {
            cuerpo.Append("<p style=\"margin:0 0 1rem 0;\">")
                .Append(bloque.Replace("\n", "<br>\n", StringComparison.Ordinal))
                .Append("</p>\n");
        }

        return
            "<!DOCTYPE html>"
            + "<html lang=\"es\"><head><meta charset=\"utf-8\"><title>"
            + Nombre
            + "</title></head>"
            + "<body style=\"margin:0;background:#f4f6f8;font-family:Segoe UI,Arial,sans-serif;color:#1a1a1a;\">"
            + "<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"background:#f4f6f8;padding:24px 0;\">"
            + "<tr><td align=\"center\">"
            + "<table role=\"presentation\" width=\"560\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:560px;background:#ffffff;border-radius:8px;\">"
            + "<tr><td style=\"background:#0f4c81;color:#ffffff;padding:20px 28px;font-size:20px;font-weight:600;\">"
            + Nombre
            + "</td></tr>"
            + "<tr><td style=\"padding:28px;font-size:16px;line-height:1.5;\">"
            + cuerpo
            + "</td></tr>"
            + "<tr><td style=\"padding:0 28px 24px;font-size:12px;color:#667085;\">"
            + "Este correo es informativo. No responda a esta dirección."
            + "</td></tr>"
            + "</table></td></tr></table></body></html>";
    }
}
