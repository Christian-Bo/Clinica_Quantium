using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Paciente
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int DocumentoMaxLength = 30;
    public const int TelefonoMaxLength = 30;
    public const int DireccionMaxLength = 250;
    public const int AlergiasMaxLength = 500;
    public const int ContactoNombreMaxLength = 150;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nombres { get; private set; } = null!;
    public string Apellidos { get; private set; } = null!;
    public string? Documento { get; private set; }
    public DateOnly? FechaNacimiento { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
    public string? Sexo { get; private set; }
    public string? Alergias { get; private set; }
    public string? ContactoEmergenciaNombre { get; private set; }
    public string? ContactoEmergenciaTelefono { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Paciente()
    {
    }

    private Paciente(
        Guid id,
        Guid usuarioId,
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        string? sexo,
        string? alergias,
        string? contactoEmergenciaNombre,
        string? contactoEmergenciaTelefono,
        bool isActive,
        DateTime createdAtUtc)
    {
        Id = id;
        UsuarioId = usuarioId;
        Nombres = nombres;
        Apellidos = apellidos;
        Documento = documento;
        FechaNacimiento = fechaNacimiento;
        Telefono = telefono;
        Direccion = direccion;
        Sexo = sexo;
        Alergias = alergias;
        ContactoEmergenciaNombre = contactoEmergenciaNombre;
        ContactoEmergenciaTelefono = contactoEmergenciaTelefono;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public static Paciente Create(
        Guid usuarioId,
        string nombres,
        string apellidos,
        string? documento = null,
        DateOnly? fechaNacimiento = null,
        string? telefono = null,
        string? direccion = null,
        string? sexo = null,
        string? alergias = null,
        string? contactoEmergenciaNombre = null,
        string? contactoEmergenciaTelefono = null)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("El paciente debe asociarse a un usuario.");
        }

        return new Paciente(
            Guid.NewGuid(),
            usuarioId,
            Obligatorio(nombres, "nombres", NombresMaxLength),
            Obligatorio(apellidos, "apellidos", ApellidosMaxLength),
            Opcional(documento, DocumentoMaxLength, "documento"),
            fechaNacimiento,
            Opcional(telefono, TelefonoMaxLength, "teléfono"),
            Opcional(direccion, DireccionMaxLength, "dirección"),
            NormalizarSexo(sexo),
            Opcional(alergias, AlergiasMaxLength, "alergias"),
            Opcional(contactoEmergenciaNombre, ContactoNombreMaxLength, "contacto de emergencia"),
            Opcional(contactoEmergenciaTelefono, TelefonoMaxLength, "teléfono de emergencia"),
            isActive: true,
            createdAtUtc: DateTime.UtcNow);
    }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public void Actualizar(
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        string? sexo = null,
        string? alergias = null,
        string? contactoEmergenciaNombre = null,
        string? contactoEmergenciaTelefono = null)
    {
        Nombres = Obligatorio(nombres, "nombres", NombresMaxLength);
        Apellidos = Obligatorio(apellidos, "apellidos", ApellidosMaxLength);
        Documento = Opcional(documento, DocumentoMaxLength, "documento");
        FechaNacimiento = fechaNacimiento;
        Telefono = Opcional(telefono, TelefonoMaxLength, "teléfono");
        Direccion = Opcional(direccion, DireccionMaxLength, "dirección");
        Sexo = NormalizarSexo(sexo);
        Alergias = Opcional(alergias, AlergiasMaxLength, "alergias");
        ContactoEmergenciaNombre = Opcional(contactoEmergenciaNombre, ContactoNombreMaxLength, "contacto de emergencia");
        ContactoEmergenciaTelefono = Opcional(contactoEmergenciaTelefono, TelefonoMaxLength, "teléfono de emergencia");
    }

    private static string? NormalizarSexo(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var sexo = valor.Trim().ToUpperInvariant();
        if (sexo is not ("M" or "F" or "X"))
        {
            throw new DomainException("El sexo debe ser M, F o X.");
        }

        return sexo;
    }

    private static string Obligatorio(string valor, string campo, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException($"El {campo} del paciente es obligatorio.");
        }

        var normalizado = valor.Trim();

        if (normalizado.Length > maxLength)
        {
            throw new DomainException($"El {campo} del paciente no puede superar {maxLength} caracteres.");
        }

        return normalizado;
    }

    private static string? Opcional(string? valor, int maxLength, string campo)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();

        if (normalizado.Length > maxLength)
        {
            throw new DomainException($"El {campo} del paciente no puede superar {maxLength} caracteres.");
        }

        return normalizado;
    }
}
