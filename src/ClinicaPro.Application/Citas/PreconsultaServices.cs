using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed record RegistrarPreconsultaInput(
    short PresionSistolicaMmHg,
    short PresionDiastolicaMmHg,
    decimal TemperaturaCelsius,
    decimal OxigenoSangrePorcentaje,
    string? Observacion);

public sealed class RegistrarPreconsultaService(
    ICitaRepository citas,
    IPreconsultaRepository preconsultas,
    IUnitOfWork unitOfWork)
{
    public async Task<Preconsulta> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        RegistrarPreconsultaInput input,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (cita.Estado is CitaEstados.Cancelada or CitaEstados.Rechazada or CitaEstados.NoPresentada)
        {
            throw new DomainException(
                $"No se pueden registrar signos vitales en una cita {cita.Estado}.");
        }

        var existente = await preconsultas.ObtenerRastreadaPorCitaAsync(citaId, cancellationToken);

        if (existente is not null)
        {
            existente.Actualizar(
                input.PresionSistolicaMmHg,
                input.PresionDiastolicaMmHg,
                input.TemperaturaCelsius,
                input.OxigenoSangrePorcentaje,
                usuarioId,
                input.Observacion);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            return existente;
        }

        var preconsulta = Preconsulta.Registrar(
            citaId,
            input.PresionSistolicaMmHg,
            input.PresionDiastolicaMmHg,
            input.TemperaturaCelsius,
            input.OxigenoSangrePorcentaje,
            usuarioId,
            input.Observacion);

        await preconsultas.AgregarAsync(preconsulta, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return preconsulta;
    }
}

public sealed class ObtenerPreconsultaService(IPreconsultaRepository preconsultas)
{
    public Task<Preconsulta?> ExecuteAsync(Guid citaId, CancellationToken cancellationToken = default)
        => preconsultas.ObtenerActivaPorCitaAsync(citaId, cancellationToken);
}