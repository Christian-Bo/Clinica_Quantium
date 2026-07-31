namespace ClinicaPro.Contracts.Common;

public sealed class ApiProblem
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
}
