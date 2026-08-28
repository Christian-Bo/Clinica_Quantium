using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Agenda;

public interface IMedicoRepository
{
    Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default);
    Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default);
    Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default);
    Task<Medico?> ObtenerPrimarioPorEspecialidadAsync(Guid especialidadId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicoEspecialidad>> ListarEspecialidadesActivasAsync(CancellationToken cancellationToken = default);
    Task AgregarAsync(Medico medico, CancellationToken cancellationToken = default);
    Task AgregarEspecialidadAsync(MedicoEspecialidad relacion, CancellationToken cancellationToken = default);
}

public interface IHorarioRepository
{
    Task<IReadOnlyList<Horario>> ListarPorMedicoAsync(Guid medicoId, CancellationToken cancellationToken = default);
    Task<Horario?> ObtenerRastreadoAsync(Guid horarioId, CancellationToken cancellationToken = default);
    Task AgregarAsync(Horario horario, CancellationToken cancellationToken = default);
}

public interface ICitaRepository
{
    Task<Cita?> ObtenerPorIdAsync(Guid citaId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarPorPacienteAsync(Guid pacienteId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarPorMedicoAsync(Guid medicoId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarPorEstadoAsync(string estado, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarEnRangoAsync(
        DateTime desde,
        DateTime hasta,
        Guid? medicoId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarQueBloqueanEnRangoAsync(
        Guid medicoId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Cita>> ListarParaRecordatorioAsync(
        DateTime desdeInicio,
        DateTime hastaInicio,
        CancellationToken cancellationToken = default);
    Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default);
}

public interface IHistorialCitaRepository
{
    Task<IReadOnlyList<HistorialCita>> ListarPorCitaAsync(Guid citaId, CancellationToken cancellationToken = default);
}

public interface IParametroRepository
{
    Task<IReadOnlyList<Parametro>> ListarActivosAsync(CancellationToken cancellationToken = default);
    Task<int> ObtenerEnteroAsync(string clave, int valorPredeterminado, CancellationToken cancellationToken = default);
    Task<Parametro?> ObtenerRastreadoAsync(string clave, CancellationToken cancellationToken = default);
}

public interface IPrepararAgendaDemo
{
    Task<string> ExecuteAsync(CancellationToken cancellationToken = default);
}
