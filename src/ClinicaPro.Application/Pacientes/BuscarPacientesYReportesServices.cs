using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public sealed class BuscarPacientesService(IPacienteRepository pacientes)
{
    public const int PageSizePorDefecto = 20;
    public const int PageSizeMaxima = 50;

    public async Task<ResultadoBusquedaPacientes> ExecuteAsync(
        string? texto,
        int page = 1,
        int pageSize = PageSizePorDefecto,
        CancellationToken cancellationToken = default)
    {
        if (page < 1)
        {
            throw new DomainException("La página debe ser mayor o igual a 1.");
        }

        if (pageSize < 1 || pageSize > PageSizeMaxima)
        {
            throw new DomainException($"El tamaño de página debe estar entre 1 y {PageSizeMaxima}.");
        }

        var omitir = (page - 1) * pageSize;
        var (items, total) = await pacientes.BuscarAsync(texto, omitir, pageSize, cancellationToken);
        return new ResultadoBusquedaPacientes(items, total, page, pageSize);
    }
}

public sealed record ResultadoBusquedaPacientes(
    IReadOnlyList<Paciente> Items,
    int Total,
    int Page,
    int PageSize);

public sealed class ListarReporteCitasService(ICitaRepository citas)
{
    public async Task<ReporteCitas> ExecuteAsync(
        DateTime? desde,
        DateTime? hasta,
        Guid? medicoId,
        CancellationToken cancellationToken = default)
    {
        var inicio = DateTime.SpecifyKind(desde ?? DateTime.Today, DateTimeKind.Unspecified);
        var fin = DateTime.SpecifyKind(hasta ?? inicio.AddDays(7), DateTimeKind.Unspecified);
        if (fin <= inicio)
        {
            throw new DomainException("El rango del reporte es inválido.");
        }

        var lista = await citas.ListarEnRangoAsync(inicio, fin, medicoId, cancellationToken);
        var porEstado = lista
            .GroupBy(cita => cita.Estado)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new ConteoEstado(grupo.Key, grupo.Count()))
            .ToList();

        return new ReporteCitas(
            inicio,
            fin,
            lista.Count,
            porEstado,
            lista.Count(cita => cita.Estado == Domain.CitaEstados.Atendida),
            lista.Count(cita => cita.Estado == Domain.CitaEstados.Cancelada),
            lista.Count(cita => cita.Estado == Domain.CitaEstados.NoPresentada),
            lista.Count(cita => cita.NumeroReprogramaciones > 0));
    }
}

public sealed record ConteoEstado(string Estado, int Cantidad);

public sealed record ReporteCitas(
    DateTime Desde,
    DateTime Hasta,
    int Total,
    IReadOnlyList<ConteoEstado> PorEstado,
    int Atendidas,
    int Canceladas,
    int NoPresentadas,
    int Reprogramadas);
