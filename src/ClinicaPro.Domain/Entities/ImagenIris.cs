using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class ImagenIris
{
    public const int LateralidadMaxLength = 20;
    public const int NombreArchivoMaxLength = 255;
    public const int TipoContenidoMaxLength = 100;
    public const int ObservacionMaxLength = 500;
    public const long TamanoMaximoBytes = 10 * 1024 * 1024;

    public static readonly string[] LateralidadesValidas =
    [
        "OD",
        "OI",
        "Ambos",
        "No especificada"
    ];

    public static readonly string[] TiposContenidoValidos =
    [
        "image/jpeg",
        "image/png",
        "image/webp"
    ];

    public Guid Id { get; private set; }
    public Guid CitaId { get; private set; }
    public Guid TomadaPorUsuarioId { get; private set; }
    public string Lateralidad { get; private set; } = null!;
    public string NombreArchivo { get; private set; } = null!;
    public string TipoContenido { get; private set; } = null!;
    public byte[] Imagen { get; private set; } = null!;
    public long? TamanoBytes { get; private set; }
    public string? HashSha256 { get; private set; }
    public string? Observacion { get; private set; }
    public DateTime FechaCapturaUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private ImagenIris()
    {
    }

    public static ImagenIris Registrar(
        Guid citaId,
        Guid tomadaPorUsuarioId,
        string nombreArchivo,
        string tipoContenido,
        byte[] imagen,
        string? lateralidad = null,
        string? hashSha256 = null,
        string? observacion = null)
    {
        if (citaId == Guid.Empty)
        {
            throw new DomainException("La imagen debe asociarse a una cita.");
        }

        if (tomadaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException("La imagen debe registrar quién la tomó.");
        }

        if (imagen is null || imagen.Length == 0)
        {
            throw new DomainException("El archivo de imagen está vacío.");
        }

        if (imagen.Length > TamanoMaximoBytes)
        {
            throw new DomainException(
                $"La imagen no puede superar {TamanoMaximoBytes / (1024 * 1024)} MB.");
        }

        var ahora = DateTime.UtcNow;

        return new ImagenIris
        {
            Id = Guid.NewGuid(),
            CitaId = citaId,
            TomadaPorUsuarioId = tomadaPorUsuarioId,
            Lateralidad = NormalizarLateralidad(lateralidad),
            NombreArchivo = Obligatorio(nombreArchivo, "nombre del archivo", NombreArchivoMaxLength),
            TipoContenido = NormalizarTipoContenido(tipoContenido),
            Imagen = imagen,
            HashSha256 = Opcional(hashSha256, 64, "hash"),
            Observacion = Opcional(observacion, ObservacionMaxLength, "observación"),
            FechaCapturaUtc = ahora,
            IsActive = true,
            CreatedAtUtc = ahora
        };
    }

    public void Desactivar()
    {
        if (!IsActive)
        {
            throw new DomainException("La imagen ya está inactiva.");
        }

        IsActive = false;
    }

    private static string NormalizarLateralidad(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return "OI";
        }

        var normalizado = valor.Trim();
        var coincidencia = LateralidadesValidas.FirstOrDefault(
            item => item.Equals(normalizado, StringComparison.OrdinalIgnoreCase));

        if (coincidencia is null)
        {
            throw new DomainException(
                $"La lateralidad debe ser {string.Join(", ", LateralidadesValidas)}.");
        }

        return coincidencia;
    }

    private static string NormalizarTipoContenido(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException("El tipo de contenido del archivo es obligatorio.");
        }

        var normalizado = valor.Trim().ToLowerInvariant();

        if (!TiposContenidoValidos.Contains(normalizado))
        {
            throw new DomainException(
                $"El archivo debe ser {string.Join(", ", TiposContenidoValidos)}.");
        }

        return normalizado;
    }

    private static string Obligatorio(string valor, string campo, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            throw new DomainException($"El {campo} es obligatorio.");
        }

        var normalizado = valor.Trim();

        if (normalizado.Length > maxLength)
        {
            throw new DomainException($"El {campo} no puede superar {maxLength} caracteres.");
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
            throw new DomainException($"La {campo} no puede superar {maxLength} caracteres.");
        }

        return normalizado;
    }
}