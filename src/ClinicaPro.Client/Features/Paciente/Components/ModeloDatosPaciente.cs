using System.Text.RegularExpressions;

namespace ClinicaPro.Client.Features.Paciente.Components;

/// <summary>
/// Modelo que respalda el formulario de datos del paciente, tanto en el
/// registro público como en la edición del propio perfil.
///
/// Los límites de longitud y los valores válidos de sexo replican las reglas
/// de ClinicaPro.Domain.Entities.Paciente. Validar aquí no sustituye al
/// backend: solo evita un viaje al servidor para errores obvios y permite
/// señalar el campo exacto que está mal.
/// </summary>
public sealed class ModeloDatosPaciente
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int DocumentoMaxLength = 30;
    public const int TelefonoMaxLength = 30;
    public const int DireccionMaxLength = 250;
    public const int AlergiasMaxLength = 500;
    public const int ContactoNombreMaxLength = 150;

    /// <summary>Reglas de Identity configuradas en la API.</summary>
    public const int PasswordMinLength = 8;

    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string PasswordConfirmacion { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Documento { get; set; } = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    public string Telefono { get; set; } = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Alergias { get; set; } = string.Empty;
    public string ContactoEmergenciaNombre { get; set; } = string.Empty;
    public string ContactoEmergenciaTelefono { get; set; } = string.Empty;

    public static ModeloDatosPaciente DesdeFicha(PacienteDto paciente) => new()
    {
        Nombres = paciente.Nombres,
        Apellidos = paciente.Apellidos,
        Documento = paciente.Documento ?? string.Empty,
        FechaNacimiento = paciente.FechaNacimiento?.ToDateTime(TimeOnly.MinValue),
        Sexo = paciente.Sexo ?? string.Empty,
        Telefono = paciente.Telefono ?? string.Empty,
        Direccion = paciente.Direccion ?? string.Empty,
        Alergias = paciente.Alergias ?? string.Empty,
        ContactoEmergenciaNombre = paciente.ContactoEmergenciaNombre ?? string.Empty,
        ContactoEmergenciaTelefono = paciente.ContactoEmergenciaTelefono ?? string.Empty
    };

    /// <summary>
    /// Devuelve un diccionario campo → mensaje. Vacío significa que el
    /// formulario puede enviarse.
    /// </summary>
    public Dictionary<string, string> Validar(bool incluyeCredenciales)
    {
        var errores = new Dictionary<string, string>();

        ValidarObligatorio(errores, nameof(Nombres), Nombres, "Escribe tus nombres.", NombresMaxLength, "Los nombres");
        ValidarObligatorio(errores, nameof(Apellidos), Apellidos, "Escribe tus apellidos.", ApellidosMaxLength, "Los apellidos");

        ValidarOpcional(errores, nameof(Documento), Documento, DocumentoMaxLength, "El documento");
        ValidarOpcional(errores, nameof(Direccion), Direccion, DireccionMaxLength, "La dirección");
        ValidarOpcional(errores, nameof(Alergias), Alergias, AlergiasMaxLength, "Las alergias");
        ValidarOpcional(errores, nameof(ContactoEmergenciaNombre), ContactoEmergenciaNombre, ContactoNombreMaxLength, "El contacto de emergencia");
        ValidarTelefono(errores, nameof(Telefono), Telefono, "El teléfono");
        ValidarTelefono(errores, nameof(ContactoEmergenciaTelefono), ContactoEmergenciaTelefono, "El teléfono de emergencia");

        if (Sexo.Length > 0 && Sexo is not ("M" or "F" or "X"))
        {
            errores[nameof(Sexo)] = "Selecciona una opción válida.";
        }

        if (FechaNacimiento is not null)
        {
            var fecha = FechaNacimiento.Value.Date;
            if (fecha > DateTime.Today)
            {
                errores[nameof(FechaNacimiento)] = "La fecha de nacimiento no puede ser futura.";
            }
            else if (fecha.Year < 1900)
            {
                errores[nameof(FechaNacimiento)] = "Revisa el año de nacimiento.";
            }
        }

        if (incluyeCredenciales)
        {
            ValidarCredenciales(errores);
        }

        return errores;
    }

    public RegisterPacienteRequest ConstruirRegistro() => new(
        Email.Trim(),
        Password,
        Nombres.Trim(),
        Apellidos.Trim(),
        Limpio(Documento),
        Limpio(Telefono),
        Limpio(Direccion),
        FechaNacimiento is null ? null : DateOnly.FromDateTime(FechaNacimiento.Value),
        Limpio(Sexo),
        Limpio(Alergias),
        Limpio(ContactoEmergenciaNombre),
        Limpio(ContactoEmergenciaTelefono));

    public ActualizarPerfilRequest ConstruirActualizacion() => new(
        Nombres.Trim(),
        Apellidos.Trim(),
        Limpio(Documento),
        Limpio(Telefono),
        Limpio(Direccion),
        FechaNacimiento is null ? null : DateOnly.FromDateTime(FechaNacimiento.Value),
        Limpio(Sexo),
        Limpio(Alergias),
        Limpio(ContactoEmergenciaNombre),
        Limpio(ContactoEmergenciaTelefono));

    private void ValidarCredenciales(Dictionary<string, string> errores)
    {
        var email = Email.Trim();
        if (email.Length == 0)
        {
            errores[nameof(Email)] = "Escribe tu correo electrónico.";
        }
        else if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            errores[nameof(Email)] = "El correo no tiene un formato válido.";
        }

        var faltantes = ReglasPasswordFaltantes(Password);
        if (faltantes.Count > 0)
        {
            errores[nameof(Password)] = "La contraseña necesita " + string.Join(", ", faltantes) + ".";
        }

        if (PasswordConfirmacion != Password)
        {
            errores[nameof(PasswordConfirmacion)] = "Las contraseñas no coinciden.";
        }
    }

    /// <summary>
    /// Reglas de Identity que la contraseña todavía no cumple. Se listan en
    /// positivo para que el paciente sepa qué le falta, no qué hizo mal.
    /// </summary>
    public static List<string> ReglasPasswordFaltantes(string password)
    {
        var faltantes = new List<string>();

        if (password.Length < PasswordMinLength)
        {
            faltantes.Add($"al menos {PasswordMinLength} caracteres");
        }

        if (!password.Any(char.IsUpper))
        {
            faltantes.Add("una mayúscula");
        }

        if (!password.Any(char.IsLower))
        {
            faltantes.Add("una minúscula");
        }

        if (!password.Any(char.IsDigit))
        {
            faltantes.Add("un número");
        }

        if (password.All(char.IsLetterOrDigit))
        {
            faltantes.Add("un símbolo (por ejemplo . - _ * !)");
        }

        return faltantes;
    }

    private static void ValidarObligatorio(
        Dictionary<string, string> errores,
        string campo,
        string valor,
        string mensajeVacio,
        int maxLength,
        string etiqueta)
    {
        var texto = valor.Trim();
        if (texto.Length == 0)
        {
            errores[campo] = mensajeVacio;
        }
        else if (texto.Length > maxLength)
        {
            errores[campo] = $"{etiqueta} no puede pasar de {maxLength} caracteres.";
        }
    }

    private static void ValidarOpcional(
        Dictionary<string, string> errores,
        string campo,
        string valor,
        int maxLength,
        string etiqueta)
    {
        if (valor.Trim().Length > maxLength)
        {
            errores[campo] = $"{etiqueta} no puede pasar de {maxLength} caracteres.";
        }
    }

    private static void ValidarTelefono(
        Dictionary<string, string> errores,
        string campo,
        string valor,
        string etiqueta)
    {
        var texto = valor.Trim();
        if (texto.Length == 0)
        {
            return;
        }

        if (texto.Length > TelefonoMaxLength)
        {
            errores[campo] = $"{etiqueta} no puede pasar de {TelefonoMaxLength} caracteres.";
            return;
        }

        if (!Regex.IsMatch(texto, @"^[0-9+()\s-]+$"))
        {
            errores[campo] = $"{etiqueta} solo puede llevar números, espacios y los signos + ( ) -";
            return;
        }

        if (texto.Count(char.IsDigit) < 8)
        {
            errores[campo] = $"{etiqueta} debe tener al menos 8 dígitos.";
        }
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
