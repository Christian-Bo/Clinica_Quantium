namespace ClinicaPro.Domain.Entities;

public sealed class HistorialCita
{
    public long Id { get; private set; }
    public Guid CitaId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string TipoCambio { get; private set; } = null!;
    public string? EstadoAnterior { get; private set; }
    public string? EstadoNuevo { get; private set; }
    public DateTime? FechaHoraInicioAnterior { get; private set; }
    public DateTime? FechaHoraInicioNueva { get; private set; }
    public DateTime? FechaHoraFinAnterior { get; private set; }
    public DateTime? FechaHoraFinNueva { get; private set; }
    public string Motivo { get; private set; } = null!;
    public DateTime FechaCambioUtc { get; private set; }

    private HistorialCita()
    {
    }
}
