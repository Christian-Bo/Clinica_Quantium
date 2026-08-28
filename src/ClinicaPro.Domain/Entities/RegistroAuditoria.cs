namespace ClinicaPro.Domain.Entities;

public sealed class RegistroAuditoria
{
    public long Id { get; private set; }
    public Guid? UsuarioId { get; private set; }
    public string Accion { get; private set; } = null!;
    public string Entidad { get; private set; } = null!;
    public string? EntidadId { get; private set; }
    public string? Detalle { get; private set; }
    public string? DireccionIp { get; private set; }
    public string? CorrelationId { get; private set; }
    public DateTime FechaUtc { get; private set; }

    private RegistroAuditoria()
    {
    }

    public static RegistroAuditoria Crear(
        Guid? usuarioId,
        string accion,
        string entidad,
        string? entidadId,
        string? detalle)
    {
        return new RegistroAuditoria
        {
            UsuarioId = usuarioId,
            Accion = accion.Trim(),
            Entidad = entidad.Trim(),
            EntidadId = entidadId,
            Detalle = detalle,
            FechaUtc = DateTime.UtcNow
        };
    }
}
