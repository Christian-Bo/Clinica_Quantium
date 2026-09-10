using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Preconsulta
{
    public const int ObservacionMaxLength = 1000;

    public const short PresionSistolicaMinima = 60;
    public const short PresionSistolicaMaxima = 250;
    public const short PresionDiastolicaMinima = 40;
    public const short PresionDiastolicaMaxima = 150;
    public const decimal TemperaturaMinima = 30m;
    public const decimal TemperaturaMaxima = 45m;
    public const decimal OxigenoMinimo = 50m;
    public const decimal OxigenoMaximo = 100m;

    public Guid Id { get; private set; }
    public Guid CitaId { get; private set; }
    public short PresionSistolicaMmHg { get; private set; }
    public short PresionDiastolicaMmHg { get; private set; }
    public decimal TemperaturaCelsius { get; private set; }
    public decimal OxigenoSangrePorcentaje { get; private set; }
    public string? Observacion { get; private set; }
    public Guid RegistradaPorUsuarioId { get; private set; }
    public DateTime FechaRegistroUtc { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Preconsulta()
    {
    }

    public static Preconsulta Registrar(
        Guid citaId,
        short presionSistolica,
        short presionDiastolica,
        decimal temperaturaCelsius,
        decimal oxigenoSangrePorcentaje,
        Guid registradaPorUsuarioId,
        string? observacion = null)
    {
        if (citaId == Guid.Empty)
        {
            throw new DomainException("La preconsulta debe asociarse a una cita.");
        }

        if (registradaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException("La preconsulta debe registrar quién tomó los datos.");
        }

        ValidarSignos(presionSistolica, presionDiastolica, temperaturaCelsius, oxigenoSangrePorcentaje);

        var ahora = DateTime.UtcNow;

        return new Preconsulta
        {
            Id = Guid.NewGuid(),
            CitaId = citaId,
            PresionSistolicaMmHg = presionSistolica,
            PresionDiastolicaMmHg = presionDiastolica,
            TemperaturaCelsius = temperaturaCelsius,
            OxigenoSangrePorcentaje = oxigenoSangrePorcentaje,
            Observacion = Opcional(observacion),
            RegistradaPorUsuarioId = registradaPorUsuarioId,
            FechaRegistroUtc = ahora,
            IsActive = true,
            CreatedAtUtc = ahora
        };
    }

    public void Actualizar(
        short presionSistolica,
        short presionDiastolica,
        decimal temperaturaCelsius,
        decimal oxigenoSangrePorcentaje,
        Guid registradaPorUsuarioId,
        string? observacion = null)
    {
        if (!IsActive)
        {
            throw new DomainException("No se puede modificar una preconsulta inactiva.");
        }

        if (registradaPorUsuarioId == Guid.Empty)
        {
            throw new DomainException("La preconsulta debe registrar quién tomó los datos.");
        }

        ValidarSignos(presionSistolica, presionDiastolica, temperaturaCelsius, oxigenoSangrePorcentaje);

        PresionSistolicaMmHg = presionSistolica;
        PresionDiastolicaMmHg = presionDiastolica;
        TemperaturaCelsius = temperaturaCelsius;
        OxigenoSangrePorcentaje = oxigenoSangrePorcentaje;
        Observacion = Opcional(observacion);
        RegistradaPorUsuarioId = registradaPorUsuarioId;
        FechaRegistroUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Desactivar()
    {
        if (!IsActive)
        {
            throw new DomainException("La preconsulta ya está inactiva.");
        }

        IsActive = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static void ValidarSignos(
        short presionSistolica,
        short presionDiastolica,
        decimal temperaturaCelsius,
        decimal oxigenoSangrePorcentaje)
    {
        if (presionSistolica is < PresionSistolicaMinima or > PresionSistolicaMaxima)
        {
            throw new DomainException(
                $"La presión sistólica debe estar entre {PresionSistolicaMinima} y {PresionSistolicaMaxima} mmHg.");
        }

        if (presionDiastolica is < PresionDiastolicaMinima or > PresionDiastolicaMaxima)
        {
            throw new DomainException(
                $"La presión diastólica debe estar entre {PresionDiastolicaMinima} y {PresionDiastolicaMaxima} mmHg.");
        }

        if (presionDiastolica >= presionSistolica)
        {
            throw new DomainException("La presión diastólica debe ser menor que la sistólica.");
        }

        if (temperaturaCelsius < TemperaturaMinima || temperaturaCelsius > TemperaturaMaxima)
        {
            throw new DomainException(
                $"La temperatura debe estar entre {TemperaturaMinima} y {TemperaturaMaxima} grados Celsius.");
        }

        if (oxigenoSangrePorcentaje < OxigenoMinimo || oxigenoSangrePorcentaje > OxigenoMaximo)
        {
            throw new DomainException(
                $"El oxígeno en sangre debe estar entre {OxigenoMinimo} y {OxigenoMaximo} por ciento.");
        }
    }

    private static string? Opcional(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var normalizado = valor.Trim();

        if (normalizado.Length > ObservacionMaxLength)
        {
            throw new DomainException(
                $"La observación no puede superar {ObservacionMaxLength} caracteres.");
        }

        return normalizado;
    }
}