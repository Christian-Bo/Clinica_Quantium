using ClinicaPro.Application.Citas;
using ClinicaPro.Domain;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Persistence.Repositories;

public sealed class ActorConsulta(ClinicaProDbContext dbContext) : IActorConsulta
{
    public async Task<IReadOnlyDictionary<Guid, ActorResumen>> ObtenerPorIdsAsync(
        IReadOnlyCollection<Guid> usuarioIds,
        CancellationToken cancellationToken = default)
    {
        var ids = usuarioIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<Guid, ActorResumen>();
        }

        var usuarios = await dbContext.Users.AsNoTracking()
            .Where(usuario => ids.Contains(usuario.Id))
            .Select(usuario => new { usuario.Id, usuario.Email })
            .ToListAsync(cancellationToken);

        var pacientes = await dbContext.Pacientes.AsNoTracking()
            .Where(paciente => ids.Contains(paciente.UsuarioId))
            .Select(paciente => new { paciente.UsuarioId, paciente.Nombres, paciente.Apellidos })
            .ToListAsync(cancellationToken);

        var medicos = await dbContext.Medicos.AsNoTracking()
            .Where(medico => ids.Contains(medico.UsuarioId))
            .Select(medico => new { medico.UsuarioId, medico.Nombres, medico.Apellidos })
            .ToListAsync(cancellationToken);

        var roles = await (
            from vinculo in dbContext.UserRoles.AsNoTracking()
            join rol in dbContext.Roles.AsNoTracking() on vinculo.RoleId equals rol.Id
            where ids.Contains(vinculo.UserId)
            select new { vinculo.UserId, rol.Name }).ToListAsync(cancellationToken);

        var mapa = new Dictionary<Guid, ActorResumen>();
        foreach (var usuario in usuarios)
        {
            var rol = ElegirRol(roles.Where(item => item.UserId == usuario.Id).Select(item => item.Name));
            var paciente = pacientes.FirstOrDefault(item => item.UsuarioId == usuario.Id);
            var medico = medicos.FirstOrDefault(item => item.UsuarioId == usuario.Id);
            var nombre = paciente is not null
                ? $"{paciente.Nombres} {paciente.Apellidos}".Trim()
                : medico is not null
                    ? $"{medico.Nombres} {medico.Apellidos}".Trim()
                    : usuario.Email ?? "Usuario";

            mapa[usuario.Id] = new ActorResumen(usuario.Id, nombre, rol);
        }

        return mapa;
    }

    private static string ElegirRol(IEnumerable<string?> nombres)
    {
        var lista = nombres.Where(nombre => !string.IsNullOrWhiteSpace(nombre)).Select(nombre => nombre!).ToList();
        foreach (var rol in new[] { RolNombres.Administrador, RolNombres.Secretaria, RolNombres.Medico, RolNombres.Paciente })
        {
            if (lista.Contains(rol))
            {
                return rol;
            }
        }

        return lista.FirstOrDefault() ?? "Usuario";
    }
}
