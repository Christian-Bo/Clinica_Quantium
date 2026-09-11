namespace ClinicaPro.Api.Controllers;

public sealed class SubirImagenIrisForm
{
    public IFormFile Archivo { get; set; } = null!;
    public string? Lateralidad { get; set; }
    public string? Observacion { get; set; }
}