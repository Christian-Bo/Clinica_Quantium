using ClinicaPro.Client.Features.Expediente.Models;

namespace ClinicaPro.Client.Features.Expediente.Services;

public sealed class ExpedienteApiService(ApiClient api)
{
    public Task<ResultadoOperacion<ExpedientePacienteClientDto>> ObtenerPacienteAsync(
        Guid pacienteId,
        CancellationToken ct = default)
        => api.ObtenerResultadoAsync<ExpedientePacienteClientDto>(
            $"api/pacientes/{pacienteId}/expediente",
            "No fue posible cargar el expediente clínico.",
            ct);

    public Task<ResultadoOperacion<ExpedienteCitaClientDto>> ObtenerCitaAsync(
        Guid citaId,
        CancellationToken ct = default)
        => api.ObtenerResultadoAsync<ExpedienteCitaClientDto>(
            $"api/citas/{citaId}/expediente",
            "No fue posible cargar el detalle de esta atención.",
            ct);

    public Task<IReadOnlyList<ImagenIrisClientDto>> ListarIrisAsync(
        Guid citaId,
        CancellationToken ct = default)
        => api.ObtenerListaAsync<ImagenIrisClientDto>(
            $"api/citas/{citaId}/imagenes-iris",
            "No fue posible cargar las capturas de iris.",
            ct,
            notFoundComoVacio: true);

    public Task<ResultadoOperacion<(byte[] Bytes, string Tipo)>> DescargarIrisAsync(
        Guid imagenIrisId,
        CancellationToken ct = default)
        => api.ObtenerArchivoAsync(
            $"api/imagenes-iris/{imagenIrisId}/archivo",
            "No fue posible cargar la captura de iris.",
            ct);

    public async Task<ResultadoOperacion<ImagenIrisClientDto>> SubirIrisAsync(
        Guid citaId,
        byte[] contenido,
        string nombreArchivo,
        string tipoContenido,
        string lateralidad,
        string? observacion,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        using var archivo = new ByteArrayContent(contenido);
        archivo.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(tipoContenido);
        form.Add(archivo, "archivo", nombreArchivo);
        form.Add(new StringContent(lateralidad), "lateralidad");

        if (!string.IsNullOrWhiteSpace(observacion))
        {
            form.Add(new StringContent(observacion.Trim()), "observacion");
        }

        return await api.EnviarMultipartAsync<ImagenIrisClientDto>(
            $"api/citas/{citaId}/imagenes-iris",
            form,
            "No fue posible guardar la captura de iris.",
            ct);
    }

    public Task<ResultadoOperacion<bool>> QuitarIrisAsync(
        Guid imagenIrisId,
        CancellationToken ct = default)
        => api.EnviarSinContenidoAsync(
            HttpMethod.Delete,
            $"api/imagenes-iris/{imagenIrisId}",
            null,
            "No fue posible quitar la captura de iris.",
            ct);
}
