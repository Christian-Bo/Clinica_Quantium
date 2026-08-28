namespace ClinicaPro.Contracts.Reportes;

public sealed record ConteoEstadoDto(string Estado, int Cantidad);

public sealed record ReporteCitasDto(
    DateTime Desde,
    DateTime Hasta,
    int Total,
    IReadOnlyList<ConteoEstadoDto> PorEstado,
    int Atendidas,
    int Canceladas,
    int NoPresentadas,
    int Reprogramadas);
