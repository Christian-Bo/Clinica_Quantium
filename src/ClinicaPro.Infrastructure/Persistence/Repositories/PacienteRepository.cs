using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class PacienteRepository(ClinicaProDbContext dbContext) : IPacienteRepository
{
    public Task<Paciente?> ObtenerPorIdAsync(Guid pacienteId, CancellationToken cancellationToken = default)
    {
        return dbContext.Pacientes.AsNoTracking()
            .SingleOrDefaultAsync(paciente => paciente.Id == pacienteId && paciente.IsActive, cancellationToken);
    }

    public async Task<Paciente?> ObtenerPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Pacientes
            .AsNoTracking()
            .SingleOrDefaultAsync(
                paciente => paciente.UsuarioId == usuarioId && paciente.IsActive,
                cancellationToken);
    }

    public Task<Paciente?> ObtenerRastreadoPorUsuarioIdAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Pacientes
            .SingleOrDefaultAsync(
                paciente => paciente.UsuarioId == usuarioId && paciente.IsActive,
                cancellationToken);
    }

    public Task<Paciente?> ObtenerRastreadoPorIdAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        return dbContext.Pacientes
            .SingleOrDefaultAsync(
                paciente => paciente.Id == pacienteId && paciente.IsActive,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Paciente>> ListarPorIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Pacientes.AsNoTracking()
            .Where(paciente => ids.Contains(paciente.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> ObtenerEmailPorPacienteIdAsync(
        Guid pacienteId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from paciente in dbContext.Pacientes.AsNoTracking()
            join usuario in dbContext.Users.AsNoTracking() on paciente.UsuarioId equals usuario.Id
            where paciente.Id == pacienteId && paciente.IsActive
            select usuario.Email)            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Paciente>> BuscarAsync(
        string? texto,
        int cantidadMaxima,
        CancellationToken cancellationToken = default)
    {
        var consulta = dbContext.Pacientes.AsNoTracking()
            .Where(paciente => paciente.IsActive);

        var termino = texto?.Trim();
        if (!string.IsNullOrEmpty(termino))
        {
            consulta = consulta.Where(paciente =>
                paciente.Nombres.Contains(termino)
                || paciente.Apellidos.Contains(termino)
                || (paciente.Documento != null && paciente.Documento.Contains(termino))
                || (paciente.Telefono != null && paciente.Telefono.Contains(termino)));
        }

        return await consulta
            .OrderBy(paciente => paciente.Apellidos)
            .ThenBy(paciente => paciente.Nombres)
            .Take(cantidadMaxima)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExisteDocumentoAsync(
        string documento,
        Guid? exceptoPacienteId,
        CancellationToken cancellationToken = default)
    {
        var valor = documento.Trim();
        return dbContext.Pacientes.AsNoTracking().AnyAsync(
            paciente =>
                paciente.Documento == valor
                && (exceptoPacienteId == null || paciente.Id != exceptoPacienteId),
            cancellationToken);
    }

    public async Task AgregarAsync(Paciente paciente, CancellationToken cancellationToken = default)
    {
        await dbContext.Pacientes.AddAsync(paciente, cancellationToken);
    }
}
