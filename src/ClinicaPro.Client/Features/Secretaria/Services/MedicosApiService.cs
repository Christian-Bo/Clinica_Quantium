namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class MedicosApiService(ApiClient api, MedicosCacheService cache)
{
    public async Task<IReadOnlyList<MedicoDto>> ListarAsync(
        CancellationToken ct = default,
        bool forzarActualizacion = false)
    {
        if (!forzarActualizacion && cache.IntentarObtener(out var almacenados))
        {
            return almacenados;
        }

        var medicos = await api.ObtenerListaAsync<MedicoDto>(
            "api/medicos",
            "No fue posible cargar los médicos.",
            ct);

        cache.Guardar(medicos);
        return medicos;
    }

    public Task<IReadOnlyList<HorarioDto>> ListarHorariosAsync(Guid medicoId, CancellationToken ct = default)
        => api.ObtenerListaAsync<HorarioDto>(
            $"api/medicos/{medicoId}/horarios",
            "No fue posible cargar los horarios del médico.",
            ct);
}
