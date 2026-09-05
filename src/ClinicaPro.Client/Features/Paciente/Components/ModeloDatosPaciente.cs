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

    /// <summary>Largo exacto de un teléfono guatemalteco.</summary>
    public const int TelefonoDigitos = 8;

    /// <summary>Largo exacto del CUI (DPI) guatemalteco.</summary>
    public const int DocumentoDigitos = 13;
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
    /// <summary>
    /// DPI. El setter descarta todo lo que no sea dígito, así que la letra
    /// simplemente no aparece al teclearla. Validar al enviar no alcanza:
    /// el paciente escribe el número entero antes de enterarse del error.
    /// </summary>
    public string Documento
    {
        get => documento;
        set => documento = SoloDigitos(value, 13);
    }
    private string documento = string.Empty;
    public DateTime? FechaNacimiento { get; set; }
    public string Sexo { get; set; } = string.Empty;
    /// <summary>Teléfono de Guatemala: solo dígitos, tope de ocho.</summary>
    public string Telefono
    {
        get => telefono;
        set => telefono = SoloDigitos(value, TelefonoDigitos);
    }
    private string telefono = string.Empty;
    public string Direccion { get; set; } = string.Empty;
    public string Alergias { get; set; } = string.Empty;
    public string ContactoEmergenciaNombre { get; set; } = string.Empty;
    public string ContactoEmergenciaTelefono
    {
        get => contactoEmergenciaTelefono;
        set => contactoEmergenciaTelefono = SoloDigitos(value, TelefonoDigitos);
    }
    private string contactoEmergenciaTelefono = string.Empty;

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

        ValidarDocumento(errores);
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

    /// <summary>
    /// Arma la petición de registro. Los tres últimos datos son la primera
    /// cita: la API los exige porque crea la cuenta y la cita en la misma
    /// transacción, y rechaza el registro completo si falta cualquiera.
    /// </summary>
    public RegisterPacienteRequest ConstruirRegistro(
        Guid? medicoId = null,
        DateTime? fechaHoraInicio = null,
        string? motivoConsulta = null) => new(
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
        Limpio(ContactoEmergenciaTelefono),
        medicoId,
        fechaHoraInicio,
        Limpio(motivoConsulta));

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

        // Formato de Guatemala: ocho dígitos exactos. Se rechaza lo demás en
        // lugar de limpiarlo por dentro, para que el paciente vea con qué se
        // quedó guardado el sistema.
        if (!Regex.IsMatch(texto, @"^[0-9]{" + TelefonoDigitos + "}$"))
        {
            errores[campo] = $"{etiqueta} debe ser de {TelefonoDigitos} dígitos, sin letras ni espacios.";
        }
    }

    /// <summary>
    /// El DPI es opcional: un extranjero se registra sin él y presenta el
    /// pasaporte en recepción. Pero si lo escribe, tiene que ser un CUI real,
    /// no trece dígitos cualesquiera.
    /// </summary>
    private void ValidarDocumento(Dictionary<string, string> errores)
    {
        var texto = Documento.Trim();
        if (texto.Length == 0)
        {
            return;
        }

        if (!Regex.IsMatch(texto, @"^[0-9]{" + DocumentoDigitos + "}$"))
        {
            errores[nameof(Documento)] = $"El DPI son {DocumentoDigitos} dígitos, sin letras ni espacios.";
            return;
        }

        if (!EsCuiValido(texto))
        {
            errores[nameof(Documento)] = "Ese DPI no es válido. Revisa que no falte ningún dígito.";
        }
    }

    /// <summary>
    /// Comprueba el dígito de control del CUI guatemalteco.
    ///
    /// El número lleva 8 dígitos correlativos, 1 verificador, 2 de departamento
    /// y 2 de municipio. El verificador sale de multiplicar cada uno de los
    /// primeros ocho dígitos por su posición (2 a 9), sumar y sacar módulo 11.
    ///
    /// Solo se comprueba ese dígito. Validar además que el departamento y el
    /// municipio existan exigiría una tabla que el país ha ido cambiando al
    /// crear municipios nuevos, y una tabla vieja rechazaría DPIs legítimos:
    /// peor que no validarlos.
    /// </summary>
    public static bool EsCuiValido(string cui)
    {
        if (cui.Length != 13 || !cui.All(char.IsDigit))
        {
            return false;
        }

        var correlativo = cui[..8];
        var verificador = cui[8] - '0';

        // Un correlativo en ceros pasaría la fórmula (0 % 11 == 0) pero no
        // corresponde a ninguna persona.
        if (correlativo.All(digito => digito == '0'))
        {
            return false;
        }

        var suma = 0;
        for (var i = 0; i < 8; i++)
        {
            suma += (correlativo[i] - '0') * (i + 2);
        }

        var esperado = suma % 11;

        // El resto 10 no cabe en un solo dígito: ese correlativo no se emite.
        return esperado < 10 && esperado == verificador;
    }

    /// <summary>
    /// Deja solo dígitos y recorta al tope. Se aplica en el setter para que
    /// pegar un número con guiones o espacios también quede limpio.
    /// </summary>
    private static string SoloDigitos(string? valor, int tope)
    {
        if (string.IsNullOrEmpty(valor))
        {
            return string.Empty;
        }

        var digitos = new string([.. valor.Where(char.IsDigit)]);
        return digitos.Length > tope ? digitos[..tope] : digitos;
    }

    private static string? Limpio(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
