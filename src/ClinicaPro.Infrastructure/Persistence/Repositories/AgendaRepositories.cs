using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class MedicoRepository(ClinicaProDbContext dbContext) : IMedicoRepository
{
    public Task<Medico?> ObtenerPorIdAsync(Guid medicoId, CancellationToken cancellationToken = default)
    {
        return dbContext.Medicos.AsNoTracking()
            .FirstOrDefaultAsync(medico => medico.Id == medicoId && medico.IsActive, cancellationToken);
    }

    public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.Medicos.AsNoTracking()
            .FirstOrDefaultAsync(medico => medico.UsuarioId == usuarioId && medico.IsActive, cancellationToken);
    }

    public async Task<Medico?> ObtenerPrimarioPorEspecialidadAsync(
        Guid especialidadId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from relacion in dbContext.MedicoEspecialidades.AsNoTracking()
            join medico in dbContext.Medicos.AsNoTracking() on relacion.MedicoId equals medico.Id
            where relacion.EspecialidadId == especialidadId
                  && relacion.EsPrimario
                  && relacion.IsActive
                  && medico.IsActive
            select medico).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Medicos.AsNoTracking()
            .Where(medico => medico.IsActive)
            .OrderBy(medico => medico.Apellidos)
            .ThenBy(medico => medico.Nombres)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicoEspecialidad>> ListarEspecialidadesActivasAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.MedicoEspecialidades.AsNoTracking()
            .Where(relacion => relacion.IsActive)
            .ToListAsync(cancellationToken);
    }
}

public sealed class HorarioRepository(ClinicaProDbContext dbContext) : IHorarioRepository
{
    public async Task<IReadOnlyList<Horario>> ListarPorMedicoAsync(
        Guid medicoId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Horarios.AsNoTracking()
            .Where(horario => horario.MedicoId == medicoId && horario.IsActive)
            .OrderBy(horario => horario.DiaSemana)
            .ThenBy(horario => horario.HoraInicio)
            .ToListAsync(cancellationToken);
    }
}

public sealed class CitaRepository(ClinicaProDbContext dbContext) : ICitaRepository
{
    public Task<Cita?> ObtenerPorIdAsync(Guid citaId, CancellationToken cancellationToken = default)
    {
        return dbContext.Citas.FirstOrDefaultAsync(cita => cita.Id == citaId, cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarPorPacienteAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Citas.AsNoTracking()
            .Where(cita => cita.PacienteId == pacienteId)
            .OrderByDescending(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarPorMedicoAsync(
        Guid medicoId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Citas.AsNoTracking()
            .Where(cita => cita.MedicoId == medicoId)
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarPorEstadoAsync(
        string estado,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Citas.AsNoTracking()
            .Where(cita => cita.Estado == estado)
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarEnRangoAsync(
        DateTime desde,
        DateTime hasta,
        Guid? medicoId,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.Citas.AsNoTracking()
            .Where(cita => cita.FechaHoraInicio >= desde && cita.FechaHoraInicio < hasta);

        if (medicoId is not null)
        {
            consulta = consulta.Where(cita => cita.MedicoId == medicoId);
        }

        return await consulta
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Cita cita, CancellationToken cancellationToken = default)
    {
        await dbContext.Citas.AddAsync(cita, cancellationToken);
    }
}

public sealed class HistorialCitaRepository(ClinicaProDbContext dbContext) : IHistorialCitaRepository
{
    public async Task<IReadOnlyList<HistorialCita>> ListarPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.HistorialCitas.AsNoTracking()
            .Where(historial => historial.CitaId == citaId)
            .OrderBy(historial => historial.Id)
            .ToListAsync(cancellationToken);
    }
}

public sealed class ParametroRepository(ClinicaProDbContext dbContext) : IParametroRepository
{
    public async Task<IReadOnlyList<Parametro>> ListarActivosAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Parametros.AsNoTracking()
            .Where(parametro => parametro.IsActive)
            .OrderBy(parametro => parametro.Clave)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ObtenerEnteroAsync(
        string clave,
        int valorPredeterminado,
        CancellationToken cancellationToken = default)
    {
        var parametro = await dbContext.Parametros.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Clave == clave && item.IsActive, cancellationToken);

        return parametro is not null && int.TryParse(parametro.Valor, out var valor)
            ? valor
            : valorPredeterminado;
    }
}
