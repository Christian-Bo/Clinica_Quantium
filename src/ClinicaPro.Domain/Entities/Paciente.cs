using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Paciente
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int DocumentoMaxLength = 30;
    public const int TelefonoMaxLength = 30;
    public const int DireccionMaxLength = 250;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nombres { get; private set; } = null!;
    public string Apellidos { get; private set; } = null!;
    public string? Documento { get; private set; }
    public DateOnly? FechaNacimiento { get; private set; }
    public string? Telefono { get; private set; }
    public string? Direccion { get; private set; }
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
        string? direccion = null)
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
            isActive: true,
            createdAtUtc: DateTime.UtcNow);
    }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

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
