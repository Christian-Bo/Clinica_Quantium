using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Medico
{
    public const int NombresMaxLength = 100;
    public const int ApellidosMaxLength = 100;
    public const int ColegiadoMaxLength = 50;
    public const int TelefonoMaxLength = 30;

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Nombres { get; private set; } = null!;
    public string Apellidos { get; private set; } = null!;
    public string? NumeroColegiado { get; private set; }
    public string? Telefono { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Medico()
    {
    }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public static Medico Create(
        Guid id,
        Guid usuarioId,
        string nombres,
        string apellidos,
        string? numeroColegiado = null,
        string? telefono = null)
    {
        if (usuarioId == Guid.Empty)
        {
            throw new DomainException("El médico debe asociarse a un usuario.");
        }

        if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
        {
            throw new DomainException("El médico requiere nombres y apellidos.");
        }

        return new Medico
        {
            Id = id == Guid.Empty ? Guid.NewGuid() : id,
            UsuarioId = usuarioId,
            Nombres = nombres.Trim(),
            Apellidos = apellidos.Trim(),
            NumeroColegiado = string.IsNullOrWhiteSpace(numeroColegiado) ? null : numeroColegiado.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Actualizar(string nombres, string apellidos, string? numeroColegiado, string? telefono)
    {
        if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
        {
            throw new DomainException("El médico requiere nombres y apellidos.");
        }

        Nombres = nombres.Trim();
        Apellidos = apellidos.Trim();
        NumeroColegiado = string.IsNullOrWhiteSpace(numeroColegiado) ? null : numeroColegiado.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
    }

    public void CambiarActivo(bool activo) => IsActive = activo;
}
