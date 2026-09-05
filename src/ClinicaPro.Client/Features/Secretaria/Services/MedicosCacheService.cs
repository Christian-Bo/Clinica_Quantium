namespace ClinicaPro.Client.Features.Secretaria.Services;

/// <summary>
/// Caché exclusivamente en memoria para un catálogo de baja sensibilidad y
/// baja frecuencia de cambio. Nunca persiste pacientes, historiales ni datos clínicos.
/// </summary>
public sealed class MedicosCacheService
{
    private static readonly TimeSpan Vigencia = TimeSpan.FromMinutes(3);
    private IReadOnlyList<MedicoDto>? valor;
    private DateTimeOffset validoHasta;

    public bool IntentarObtener(out IReadOnlyList<MedicoDto> medicos)
    {
        if (valor is not null && DateTimeOffset.UtcNow < validoHasta)
        {
            medicos = valor;
            return true;
        }

        medicos = [];
        return false;
    }

    public void Guardar(IReadOnlyList<MedicoDto> medicos)
    {
        valor = medicos;
        validoHasta = DateTimeOffset.UtcNow.Add(Vigencia);
    }

    public void Invalidar()
    {
        valor = null;
        validoHasta = default;
    }
}
