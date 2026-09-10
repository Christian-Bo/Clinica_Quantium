using System.Net.Mail;
using System.Text;

namespace ClinicaPro.Client.Shared.UI.Forms;

public enum FormFieldKind
{
    Text,
    Name,
    Email,
    Dpi,
    GuatemalaPhone,
    Collegiate
}

/// <summary>
/// Reglas de presentación y normalización del frontend. Estas validaciones
/// mejoran la UX y nunca sustituyen las reglas/validaciones de la API.
/// </summary>
public static class FormValidation
{
    public const int DpiLength = 13;
    public const int GuatemalaPhoneLength = 8;
    public const int PasswordMinLength = 8;
    public const int MotivoMinLength = 5;
    public const int MotivoMaxLength = 500;

    public static string Normalize(FormFieldKind kind, string? value, int? maxLength = null)
    {
        value ??= string.Empty;

        var normalized = kind switch
        {
            FormFieldKind.Dpi => Digits(value, DpiLength),
            FormFieldKind.GuatemalaPhone => NormalizeGuatemalaPhoneInput(value),
            FormFieldKind.Email => RemoveWhitespace(value).ToLowerInvariant(),
            _ => value
        };

        if (maxLength is > 0 && normalized.Length > maxLength.Value)
        {
            normalized = normalized[..maxLength.Value];
        }

        return normalized;
    }

    public static string? Validate(
        FormFieldKind kind,
        string? value,
        bool required,
        int? maxLength = null,
        string? label = null)
    {
        var text = value?.Trim() ?? string.Empty;
        var nombreCampo = string.IsNullOrWhiteSpace(label) ? "Este campo" : label;

        if (string.IsNullOrWhiteSpace(text))
        {
            return required ? $"{nombreCampo} es obligatorio." : null;
        }

        if (maxLength is > 0 && text.Length > maxLength.Value)
        {
            return $"{nombreCampo} no puede superar {maxLength.Value} caracteres.";
        }

        return kind switch
        {
            FormFieldKind.Name => ValidateName(text, nombreCampo),
            FormFieldKind.Email => IsEmailValid(text) ? null : "Ingresa un correo con formato válido, por ejemplo usuario@dominio.com.",
            FormFieldKind.Dpi => text.Length == DpiLength && text.All(char.IsDigit)
                ? null
                : $"El DPI/CUI debe contener exactamente {DpiLength} dígitos.",
            FormFieldKind.GuatemalaPhone => text.Length == GuatemalaPhoneLength && text.All(char.IsDigit)
                ? null
                : $"El teléfono debe contener exactamente {GuatemalaPhoneLength} dígitos después de +502.",
            _ => null
        };
    }

    public static string? ValidateText(
        string? value,
        bool required,
        int minLength,
        int maxLength,
        string label)
    {
        var text = value?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            return required ? $"{label} es obligatorio." : null;
        }
        if (text.Length < minLength)
        {
            return $"{label} debe tener al menos {minLength} caracteres.";
        }
        if (text.Length > maxLength)
        {
            return $"{label} no puede superar {maxLength} caracteres.";
        }
        return null;
    }

    public static IReadOnlyList<string> MissingPasswordRules(string? password)
    {
        password ??= string.Empty;
        var missing = new List<string>();
        if (password.Length < PasswordMinLength) missing.Add($"{PasswordMinLength} caracteres");
        if (!password.Any(char.IsUpper)) missing.Add("una mayúscula");
        if (!password.Any(char.IsLower)) missing.Add("una minúscula");
        if (!password.Any(char.IsDigit)) missing.Add("un número");
        if (!password.Any(c => !char.IsLetterOrDigit(c))) missing.Add("un símbolo");
        return missing;
    }

    public static bool IsPasswordValid(string? password) => MissingPasswordRules(password).Count == 0;

    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        return string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    public static string NormalizeEmail(string? value)
        => RemoveWhitespace(value ?? string.Empty).ToLowerInvariant();

    public static string NormalizeDpi(string? value) => Digits(value ?? string.Empty, DpiLength);

    public static string NormalizeGuatemalaPhoneInput(string? value)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.StartsWith("502", StringComparison.Ordinal) && digits.Length > GuatemalaPhoneLength)
        {
            digits = digits[3..];
        }
        if (digits.Length > GuatemalaPhoneLength)
        {
            digits = digits[..GuatemalaPhoneLength];
        }
        return digits;
    }

    public static string? ToApiGuatemalaPhone(string? value)
    {
        var local = NormalizeGuatemalaPhoneInput(value);
        return string.IsNullOrWhiteSpace(local) ? null : $"+502{local}";
    }

    public static string FromApiGuatemalaPhone(string? value)
        => NormalizeGuatemalaPhoneInput(value);

    public static string FormatGuatemalaPhone(string? value)
    {
        var local = NormalizeGuatemalaPhoneInput(value);
        return local.Length == GuatemalaPhoneLength
            ? $"+502 {local[..4]} {local[4..]}"
            : string.IsNullOrWhiteSpace(value) ? "—" : value!.Trim();
    }

    public static bool IsEmailValid(string? value)
    {
        var email = NormalizeEmail(value);
        if (email.Length is < 5 or > 254 || email.Contains("..", StringComparison.Ordinal)) return false;
        try
        {
            var address = new MailAddress(email);
            var at = email.LastIndexOf('@');
            if (!string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase) || at <= 0) return false;
            var domain = email[(at + 1)..];
            return domain.Contains('.')
                && !domain.StartsWith(".", StringComparison.Ordinal)
                && !domain.EndsWith(".", StringComparison.Ordinal)
                && domain.Split('.').All(part => part.Length > 0);
        }
        catch
        {
            return false;
        }
    }

    private static string? ValidateName(string text, string label)
    {
        if (text.Any(char.IsDigit)) return $"{label} no debe contener números.";
        if (text.Any(c => !(char.IsLetter(c) || char.IsWhiteSpace(c) || c is '-' or '\'')))
        {
            return $"{label} solo puede contener letras, espacios, guiones o apóstrofes.";
        }
        return null;
    }

    private static string Digits(string value, int max)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length <= max ? digits : digits[..max];
    }

    private static string RemoveWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var c in value)
        {
            if (!char.IsWhiteSpace(c)) builder.Append(c);
        }
        return builder.ToString();
    }
}
