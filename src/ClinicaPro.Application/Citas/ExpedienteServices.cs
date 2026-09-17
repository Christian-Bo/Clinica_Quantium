using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Citas;

public sealed record ExpedienteCitaResultado(
    Cita Cita,
    Paciente Paciente,
    Preconsulta? Preconsulta,
    IReadOnlyList<ImagenIrisMetadato> ImagenesIris);

public sealed record ExpedienteVisitaResumen(
    Cita Cita,
    string MedicoNombre,
    Preconsulta? Preconsulta,
    int CantidadIris);

public sealed record ExpedientePacienteResultado(
    Paciente Paciente,
    IReadOnlyList<ExpedienteVisitaResumen> Visitas);

public sealed class ObtenerExpedienteCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IPreconsultaRepository preconsultas,
    IImagenIrisRepository imagenes,
    AccesoExpedienteService accesoExpediente)
{
    public async Task<ExpedienteCitaResultado?> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        bool esStaffMostrador,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken);
        if (cita is null)
        {
            return null;
        }

        var paciente = await pacientes.ObtenerPorIdAsync(cita.PacienteId, cancellationToken);
        if (paciente is null)
        {
            return null;
        }

        await accesoExpediente.ExigirLecturaAsync(
            usuarioId,
            esStaffMostrador,
            paciente.Id,
            cancellationToken);

        var preconsulta = await preconsultas.ObtenerActivaPorCitaAsync(cita.Id, cancellationToken);
        var iris = await imagenes.ListarPorCitaAsync(cita.Id, cancellationToken);

        return new ExpedienteCitaResultado(cita, paciente, preconsulta, iris);
    }
}

public sealed class ObtenerExpedientePacienteService(
    IPacienteRepository pacientes,
    ICitaRepository citas,
    IMedicoRepository medicos,
    IPreconsultaRepository preconsultas,
    IImagenIrisRepository imagenes,
    AccesoExpedienteService accesoExpediente)
{
    public async Task<ExpedientePacienteResultado?> ExecuteAsync(
        Guid pacienteId,
        Guid usuarioId,
        bool esStaffMostrador,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorIdAsync(pacienteId, cancellationToken);
        if (paciente is null)
        {
            return null;
        }

        await accesoExpediente.ExigirLecturaAsync(
            usuarioId,
            esStaffMostrador,
            paciente.Id,
            cancellationToken);

        var visitas = (await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken))
            .OrderByDescending(cita => cita.FechaHoraInicio)
            .ToList();

        var citaIds = visitas.Select(cita => cita.Id).ToList();
        var signos = (await preconsultas.ListarActivasPorCitasAsync(citaIds, cancellationToken))
            .ToDictionary(item => item.CitaId);
        var conteoIris = await imagenes.ContarActivasPorCitasAsync(citaIds, cancellationToken);
        var nombresMedico = (await medicos.ListarTodosAsync(cancellationToken))
            .ToDictionary(item => item.Id, item => item.NombreCompleto);

        var resumen = visitas.Select(cita => new ExpedienteVisitaResumen(
            cita,
            nombresMedico.GetValueOrDefault(cita.MedicoId, string.Empty),
            signos.GetValueOrDefault(cita.Id),
            conteoIris.GetValueOrDefault(cita.Id)))
            .ToList();

        return new ExpedientePacienteResultado(paciente, resumen);
    }
}
