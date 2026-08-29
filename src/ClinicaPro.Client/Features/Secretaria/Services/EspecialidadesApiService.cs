namespace ClinicaPro.Client.Features.Secretaria.Services;

public sealed class EspecialidadesApiService(HttpClient http)
{
    public async Task<IReadOnlyList<EspecialidadDto>> ListarAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<EspecialidadDto>>("api/especialidades", ct) ?? [];

    public async Task<MedicoDto?> ObtenerMedicoPrimarioAsync(Guid especialidadId, CancellationToken ct = default)
    {
        var respuesta = await http.GetAsync($"api/especialidades/{especialidadId}/medico-primario", ct);
        return respuesta.IsSuccessStatusCode
            ? await respuesta.Content.ReadFromJsonAsync<MedicoDto>(cancellationToken: ct)
            : null;
    }
}
