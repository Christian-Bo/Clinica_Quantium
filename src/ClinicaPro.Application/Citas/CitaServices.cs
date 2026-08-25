using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed record SolicitarCitaInput(Guid EspecialidadId, DateTime FechaHoraInicio, string MotivoConsulta);

public sealed class SolicitarCitaService(
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    ICitaRepository citas,
    IParametroRepository parametros,
    IUnitOfWork unitOfWork)
{
    public async Task<Cita> ExecuteAsync(
        Guid usuarioId,
        SolicitarCitaInput input,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        var medico = await medicos.ObtenerPrimarioPorEspecialidadAsync(input.EspecialidadId, cancellationToken)
            ?? throw new DomainException("La especialidad no tiene un médico primario activo. Pida a un administrador ejecutar POST /api/demo/preparar-agenda.");

        var duracion = await parametros.ObtenerEnteroAsync(
            "Citas.DuracionPredeterminadaMinutos",
            Cita.DuracionPredeterminadaMinutos,
            cancellationToken);

        var cita = Cita.Solicitar(
            paciente.Id,
            medico.Id,
            input.EspecialidadId,
            usuarioId,
            input.FechaHoraInicio,
            input.MotivoConsulta,
            duracion);

        await citas.AgregarAsync(cita, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return cita;
    }
}

public sealed class OperarCitaService(
    ICitaRepository citas,
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork)
{
    public async Task<Cita> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        cambiar(cita);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        return cita;
    }

    public async Task<Cita> ExecuteComoPacienteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        Action<Cita> cambiar,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (cita.PacienteId != paciente.Id)
        {
            throw new DomainException("La cita no pertenece al paciente autenticado.");
        }

        cambiar(cita);
        await unitOfWork.SaveChangesWithSqlSessionContextAsync(usuarioId, motivo, cancellationToken);
        return cita;
    }
}

public sealed class ListarCitasPacienteService(IPacienteRepository pacientes, ICitaRepository citas)
{
    public async Task<IReadOnlyList<Cita>> ExecuteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        return await citas.ListarPorPacienteAsync(paciente.Id, cancellationToken);
    }
}

public sealed class ListarCitasMedicoService(IMedicoRepository medicos, ICitaRepository citas)
{
    public async Task<IReadOnlyList<Cita>> ExecuteAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de médico.");

        return await citas.ListarPorMedicoAsync(medico.Id, cancellationToken);
    }
}

public sealed class ListarCitasPendientesService(ICitaRepository citas)
{
    public Task<IReadOnlyList<Cita>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return citas.ListarPorEstadoAsync(CitaEstados.Solicitada, cancellationToken);
    }
}

public sealed class ListarAgendaService(ICitaRepository citas)
{
    public Task<IReadOnlyList<Cita>> ExecuteAsync(
        DateTime? desde,
        DateTime? hasta,
        Guid? medicoId,
        CancellationToken cancellationToken = default)
    {
        var inicio = DateTime.SpecifyKind(desde ?? DateTime.Today, DateTimeKind.Unspecified);
        var fin = DateTime.SpecifyKind(hasta ?? inicio.AddDays(7), DateTimeKind.Unspecified);
        if (fin <= inicio)
        {
            throw new DomainException("El rango de agenda es inválido.");
        }

        return citas.ListarEnRangoAsync(inicio, fin, medicoId, cancellationToken);
    }
}
