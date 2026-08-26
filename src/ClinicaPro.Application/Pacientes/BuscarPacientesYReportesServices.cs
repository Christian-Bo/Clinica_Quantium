using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public sealed class BuscarPacientesService(IPacienteRepository pacientes)
{
    public Task<IReadOnlyList<Paciente>> ExecuteAsync(
        string? texto,
        CancellationToken cancellationToken = default)
    {
        return pacientes.BuscarAsync(texto, cantidadMaxima: 20, cancellationToken);
    }
}

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
