namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class MedicosApiService(HttpClient http)
{
    public async Task<IReadOnlyList<MedicoDto>> ListarAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<MedicoDto>>("api/medicos", ct) ?? [];

    public async Task<IReadOnlyList<HorarioDto>> ListarHorariosAsync(Guid medicoId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<HorarioDto>>($"api/medicos/{medicoId}/horarios", ct) ?? [];
}
