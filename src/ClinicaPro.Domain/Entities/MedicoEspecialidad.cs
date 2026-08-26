namespace ClinicaPro.Domain.Entities;

public sealed class MedicoEspecialidad
{
    public Guid MedicoId { get; private set; }
    public Guid EspecialidadId { get; private set; }
    public bool EsPrimario { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private MedicoEspecialidad()
    {
    }

    public static MedicoEspecialidad Create(Guid medicoId, Guid especialidadId, bool esPrimario)
    {
        return new MedicoEspecialidad
        {
            MedicoId = medicoId,
            EspecialidadId = especialidadId,
            EsPrimario = esPrimario,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void CambiarActivo(bool activo) => IsActive = activo;

    public void MarcarPrimario(bool esPrimario) => EsPrimario = esPrimario;
}
