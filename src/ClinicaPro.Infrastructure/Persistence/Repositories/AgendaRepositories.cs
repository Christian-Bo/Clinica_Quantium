using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
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

    public Task<Medico?> ObtenerRastreadoAsync(Guid medicoId, CancellationToken cancellationToken = default)
    {
        return dbContext.Medicos.FirstOrDefaultAsync(medico => medico.Id == medicoId, cancellationToken);
    }

    public Task<Medico?> ObtenerPorUsuarioIdAsync(Guid usuarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.Medicos.AsNoTracking()
            .FirstOrDefaultAsync(medico => medico.UsuarioId == usuarioId && medico.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Medico>> ListarActivosAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Medicos.AsNoTracking()
            .Where(medico => medico.IsActive)
            .OrderBy(medico => medico.Apellidos)
            .ThenBy(medico => medico.Nombres)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Medico>> ListarTodosAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Medicos.AsNoTracking()
            .OrderBy(medico => medico.Apellidos)
            .ThenBy(medico => medico.Nombres)
            .ToListAsync(cancellationToken);
    }

    public async Task AgregarAsync(Medico medico, CancellationToken cancellationToken = default)
    {
        await dbContext.Medicos.AddAsync(medico, cancellationToken);
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

    public Task<Horario?> ObtenerRastreadoAsync(Guid horarioId, CancellationToken cancellationToken = default)
    {
        return dbContext.Horarios.FirstOrDefaultAsync(horario => horario.Id == horarioId, cancellationToken);
    }

    public async Task AgregarAsync(Horario horario, CancellationToken cancellationToken = default)
    {
        await dbContext.Horarios.AddAsync(horario, cancellationToken);
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

    public async Task<IReadOnlyList<Cita>> ListarQueBloqueanEnRangoAsync(
        Guid medicoId,
        DateTime desde,
        DateTime hasta,
        CancellationToken cancellationToken = default)
    {
        var estados = new[]
        {
            CitaEstados.Solicitada,
            CitaEstados.Programada,
            CitaEstados.Confirmada,
            CitaEstados.EnEspera,
            CitaEstados.EnAtencion
        };

        return await dbContext.Citas.AsNoTracking()
            .Where(cita =>
                cita.MedicoId == medicoId
                && estados.Contains(cita.Estado)
                && cita.FechaHoraInicio < hasta
                && desde < cita.FechaHoraFin)
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Cita>> ListarQueBloqueanPacienteEnRangoAsync(
        Guid pacienteId,
        DateTime desde,
        DateTime hasta,
        Guid? exceptoCitaId,
        CancellationToken cancellationToken = default)
    {
        var estados = EstadosQueBloqueanHorario;

        var consulta = dbContext.Citas.AsNoTracking()
            .Where(cita =>
                cita.PacienteId == pacienteId
                && estados.Contains(cita.Estado)
                && cita.FechaHoraInicio < hasta
                && desde < cita.FechaHoraFin);

        if (exceptoCitaId is not null)
        {
            consulta = consulta.Where(cita => cita.Id != exceptoCitaId.Value);
        }

        return await consulta
            .OrderBy(cita => cita.FechaHoraInicio)
            .ToListAsync(cancellationToken);
    }

    public Task<int> ContarActivasFuturasAsync(
        Guid pacienteId,
        DateTime ahoraClinica,
        Guid? exceptoCitaId,
        CancellationToken cancellationToken = default)
    {
        var estados = EstadosQueBloqueanHorario;

        var consulta = dbContext.Citas.AsNoTracking()
            .Where(cita =>
                cita.PacienteId == pacienteId
                && estados.Contains(cita.Estado)
                && cita.FechaHoraInicio >= ahoraClinica);

        if (exceptoCitaId is not null)
        {
            consulta = consulta.Where(cita => cita.Id != exceptoCitaId.Value);
        }

        return consulta.CountAsync(cancellationToken);
    }

    private static readonly string[] EstadosQueBloqueanHorario =
    [
        CitaEstados.Solicitada,
        CitaEstados.Programada,
        CitaEstados.Confirmada,
        CitaEstados.EnEspera,
        CitaEstados.EnAtencion
    ];

    public async Task<IReadOnlyList<Cita>> ListarParaRecordatorioAsync(
        DateTime desdeInicio,
        DateTime hastaInicio,
        CancellationToken cancellationToken = default)
    {
        var estados = new[] { CitaEstados.Programada, CitaEstados.Confirmada };

        return await dbContext.Citas.AsNoTracking()
            .Where(cita =>
                estados.Contains(cita.Estado)
                && cita.FechaHoraInicio >= desdeInicio
                && cita.FechaHoraInicio < hastaInicio)
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

    public async Task AgregarAsync(HistorialCita historial, CancellationToken cancellationToken = default)
    {
        await dbContext.HistorialCitas.AddAsync(historial, cancellationToken);
    }
}

public sealed class AutorizacionReprogramacionRepository(ClinicaProDbContext dbContext) : IAutorizacionReprogramacionRepository
{
    public async Task<AutorizacionReprogramacion?> ObtenerPorIdAsync(
        Guid autorizacionId,
        CancellationToken cancellationToken = default)
        => await ObtenerPorCitaAsync(autorizacionId, cancellationToken);

    public async Task<AutorizacionReprogramacion?> ObtenerPendientePorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await ObtenerPorCitaAsync(citaId, cancellationToken);
        return autorizacion?.Estado == AutorizacionReprogramacionEstados.Pendiente ? autorizacion : null;
    }

    public async Task<AutorizacionReprogramacion?> ObtenerAprobadaPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        var autorizacion = await ObtenerPorCitaAsync(citaId, cancellationToken);
        return autorizacion?.Estado == AutorizacionReprogramacionEstados.Aprobada ? autorizacion : null;
    }

    public async Task<IReadOnlyList<AutorizacionReprogramacion>> ListarAsync(
        string? estado,
        CancellationToken cancellationToken = default)
    {
        var eventos = await ConsultaAutorizaciones().ToListAsync(cancellationToken);
        var lista = eventos
            .GroupBy(item => item.CitaId)
            .Select(DesdeHistorial)
            .OfType<AutorizacionReprogramacion>()
            .Where(item => string.IsNullOrWhiteSpace(estado) || item.Estado == estado)
            .OrderBy(item => item.Estado == AutorizacionReprogramacionEstados.Pendiente ? 0 : 1)
            .ThenByDescending(item => item.CreatedAtUtc)
            .Take(100)
            .ToList();

        return lista;
    }

    public Task AgregarAsync(AutorizacionReprogramacion autorizacion, CancellationToken cancellationToken = default)
        => RegistrarCambioAsync(autorizacion, cancellationToken);

    public async Task RegistrarCambioAsync(
        AutorizacionReprogramacion autorizacion,
        CancellationToken cancellationToken = default)
    {
        var actor = autorizacion.AutorizadaPorUsuarioId ?? autorizacion.SolicitadaPorUsuarioId;
        var motivo = autorizacion.Estado == AutorizacionReprogramacionEstados.Pendiente
            ? autorizacion.MotivoSolicitud
            : autorizacion.MotivoDecision ?? autorizacion.MotivoSolicitud;

        await dbContext.HistorialCitas.AddAsync(
            HistorialCita.RegistrarAutorizacion(autorizacion.CitaId, actor, motivo, autorizacion.Estado),
            cancellationToken);
    }

    private async Task<AutorizacionReprogramacion?> ObtenerPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken)
    {
        var eventos = await ConsultaAutorizaciones()
            .Where(item => item.CitaId == citaId)
            .ToListAsync(cancellationToken);

        return DesdeHistorial(eventos);
    }

    private IQueryable<HistorialCita> ConsultaAutorizaciones()
        => dbContext.HistorialCitas.AsNoTracking()
            .Where(item => item.TipoCambio == "Autorizacion" && item.EstadoNuevo != null);

    private static AutorizacionReprogramacion? DesdeHistorial(IEnumerable<HistorialCita> eventos)
    {
        var ordenados = eventos
            .OrderBy(item => item.FechaCambioUtc)
            .ThenBy(item => item.Id)
            .ToList();
        if (ordenados.Count == 0)
        {
            return null;
        }

        var solicitud = ordenados.FirstOrDefault(item => item.EstadoNuevo == AutorizacionReprogramacionEstados.Pendiente)
            ?? ordenados[0];
        var actual = ordenados[^1];
        var decision = ordenados.LastOrDefault(item =>
            item.EstadoNuevo is AutorizacionReprogramacionEstados.Aprobada
                or AutorizacionReprogramacionEstados.Rechazada);

        return AutorizacionReprogramacion.Reconstruir(
            solicitud.CitaId,
            solicitud.UsuarioId,
            decision?.UsuarioId,
            actual.EstadoNuevo ?? AutorizacionReprogramacionEstados.Pendiente,
            solicitud.Motivo,
            decision?.Motivo,
            solicitud.FechaCambioUtc,
            decision?.FechaCambioUtc);
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

    public Task<Parametro?> ObtenerRastreadoAsync(string clave, CancellationToken cancellationToken = default)
    {
        return dbContext.Parametros.FirstOrDefaultAsync(item => item.Clave == clave, cancellationToken);
    }
}

public sealed class PreconsultaRepository(ClinicaProDbContext dbContext) : IPreconsultaRepository
{
    public Task<Preconsulta?> ObtenerActivaPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Preconsultas.AsNoTracking()
            .FirstOrDefaultAsync(
                preconsulta => preconsulta.CitaId == citaId && preconsulta.IsActive,
                cancellationToken);
    }

    public Task<Preconsulta?> ObtenerRastreadaPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Preconsultas
            .FirstOrDefaultAsync(
                preconsulta => preconsulta.CitaId == citaId && preconsulta.IsActive,
                cancellationToken);
    }

    public async Task AgregarAsync(Preconsulta preconsulta, CancellationToken cancellationToken = default)
    {
        await dbContext.Preconsultas.AddAsync(preconsulta, cancellationToken);
    }
}

public sealed class ImagenIrisRepository(ClinicaProDbContext dbContext) : IImagenIrisRepository
{
    public async Task<IReadOnlyList<ImagenIrisMetadato>> ListarPorCitaAsync(
        Guid citaId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ImagenesIris.AsNoTracking()
            .Where(imagen => imagen.CitaId == citaId && imagen.IsActive)
            .OrderByDescending(imagen => imagen.FechaCapturaUtc)
            .Select(imagen => new ImagenIrisMetadato(
                imagen.Id,
                imagen.CitaId,
                imagen.TomadaPorUsuarioId,
                imagen.Lateralidad,
                imagen.NombreArchivo,
                imagen.TipoContenido,
                imagen.TamanoBytes,
                imagen.Observacion,
                imagen.FechaCapturaUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ImagenIrisArchivo?> ObtenerArchivoAsync(
        Guid imagenIrisId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.ImagenesIris.AsNoTracking()
            .Where(imagen => imagen.Id == imagenIrisId && imagen.IsActive)
            .Select(imagen => new ImagenIrisArchivo(
                imagen.NombreArchivo,
                imagen.TipoContenido,
                imagen.Imagen))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ImagenIris?> ObtenerRastreadaAsync(
        Guid imagenIrisId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.ImagenesIris
            .FirstOrDefaultAsync(imagen => imagen.Id == imagenIrisId, cancellationToken);
    }

    public async Task AgregarAsync(ImagenIris imagen, CancellationToken cancellationToken = default)
    {
        await dbContext.ImagenesIris.AddAsync(imagen, cancellationToken);
    }
}