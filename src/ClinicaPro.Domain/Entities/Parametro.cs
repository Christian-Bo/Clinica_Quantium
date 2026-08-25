namespace ClinicaPro.Domain.Entities;

public sealed class Parametro
{
    public string Clave { get; private set; } = null!;
    public string Valor { get; private set; } = null!;
    public string TipoDato { get; private set; } = null!;
    public string? Descripcion { get; private set; }
    public bool IsActive { get; private set; }

    private Parametro()
    {
    }
}
