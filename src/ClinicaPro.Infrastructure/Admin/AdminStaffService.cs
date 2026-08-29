using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Admin;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;
using ClinicaPro.Infrastructure.Identity;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Admin;

public sealed class AdminStaffService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IMedicoRepository medicos,
    IEspecialidadRepository especialidades,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria,
    ClinicaProDbContext dbContext) : IAdminStaffService
{
    public async Task<Medico> CrearMedicoAsync(CrearMedicoInput input, Guid adminId, CancellationToken cancellationToken)
    {
        var email = input.Email.Trim();
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            throw new DomainException("El correo ya está registrado.");
        }

        var especialidad = await especialidades.ObtenerPorIdAsync(input.EspecialidadId, cancellationToken)
            ?? throw new DomainException("La especialidad no existe o no está activa.");

        var rol = await roleManager.FindByNameAsync(RolNombres.Medico)
            ?? throw new DomainException("No existe el rol Médico.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = input.Telefono?.Trim(),
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
            LockoutEnabled = true
        };

        var creado = await userManager.CreateAsync(user, input.Password);
        if (!creado.Succeeded)
        {
            throw new DomainException("No fue posible crear el usuario del médico. Revise la contraseña.");
        }

        await userManager.AddToRoleAsync(user, RolNombres.Medico);

        var medico = Medico.Create(Guid.NewGuid(), user.Id, input.Nombres, input.Apellidos, input.NumeroColegiado, input.Telefono);
        await medicos.AgregarAsync(medico, cancellationToken);
        await medicos.AgregarEspecialidadAsync(
            MedicoEspecialidad.Create(medico.Id, especialidad.Id, input.EsPrimario),
            cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Crear", "Medico", medico.Id.ToString(), email, cancellationToken);
        return medico;
    }

    public async Task<Medico> ActualizarMedicoAsync(
        Guid medicoId,
        string nombres,
        string apellidos,
        string? colegiado,
        string? telefono,
        bool isActive,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var medico = await medicos.ObtenerRastreadoAsync(medicoId, cancellationToken)
            ?? throw new DomainException("El médico no existe.");

        medico.Actualizar(nombres, apellidos, colegiado, telefono);
        medico.CambiarActivo(isActive);

        var user = await userManager.FindByIdAsync(medico.UsuarioId.ToString());
        if (user is not null)
        {
            user.IsActive = isActive;
            user.PhoneNumber = telefono?.Trim();
            user.UpdatedAtUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "Medico", medico.Id.ToString(), null, cancellationToken);
        return medico;
    }

    public async Task<IReadOnlyList<UsuarioStaffInfo>> ListarUsuariosAsync(
        CancellationToken cancellationToken)
    {
        var usuarios = await dbContext.Users.AsNoTracking()
            .OrderBy(usuario => usuario.Email)
            .ToListAsync(cancellationToken);

        var resultado = new List<UsuarioStaffInfo>();
        foreach (var usuario in usuarios)
        {
            var roles = await userManager.GetRolesAsync(usuario);
            resultado.Add(new UsuarioStaffInfo(
                usuario.Id,
                usuario.Email ?? string.Empty,
                usuario.IsActive,
                roles.ToList()));
        }

        return resultado;
    }

    public async Task<IReadOnlyList<AdminMedicoInfo>> ListarMedicosAsync(CancellationToken cancellationToken)
    {
        var lista = await medicos.ListarTodosAsync(cancellationToken);
        var relaciones = (await medicos.ListarEspecialidadesActivasAsync(cancellationToken))
            .GroupBy(item => item.MedicoId)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.ToList());

        var usuarioIds = lista.Select(item => item.UsuarioId).ToList();
        var correos = await dbContext.Users.AsNoTracking()
            .Where(usuario => usuarioIds.Contains(usuario.Id))
            .Select(usuario => new { usuario.Id, usuario.Email })
            .ToListAsync(cancellationToken);
        var porUsuario = correos.ToDictionary(item => item.Id, item => item.Email ?? string.Empty);

        return lista.Select(medico =>
        {
            relaciones.TryGetValue(medico.Id, out var especialidades);
            especialidades ??= [];
            return new AdminMedicoInfo(
                medico.Id,
                medico.UsuarioId,
                porUsuario.GetValueOrDefault(medico.UsuarioId, string.Empty),
                medico.Nombres,
                medico.Apellidos,
                medico.NombreCompleto,
                medico.NumeroColegiado,
                medico.Telefono,
                medico.IsActive,
                especialidades.Select(item => item.EspecialidadId).ToList(),
                especialidades.FirstOrDefault(item => item.EsPrimario)?.EspecialidadId);
        }).ToList();
    }

    public async Task CambiarActivoUsuarioAsync(Guid usuarioId, bool isActive, Guid adminId, CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new DomainException("El usuario no existe.");

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(RolNombres.Administrador) && !isActive)
        {
            throw new DomainException("No se puede desactivar un administrador.");
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await auditoria.RegistrarAsync(
            adminId,
            isActive ? "Activar" : "Desactivar",
            "Usuario",
            usuarioId.ToString(),
            user.Email,
            cancellationToken);
    }

    public async Task<UsuarioStaffInfo> CrearUsuarioStaffAsync(
        string email,
        string password,
        string rol,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var rolCanonico = RolesStaffAdministrables.NormalizarUno(rol);
        var correo = (email ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(correo))
        {
            throw new DomainException("El correo es obligatorio.");
        }

        if (await userManager.FindByEmailAsync(correo) is not null)
        {
            throw new DomainException("El correo ya está registrado.");
        }

        _ = await roleManager.FindByNameAsync(rolCanonico)
            ?? throw new DomainException($"No existe el rol {rolCanonico}.");

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = correo,
            Email = correo,
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = true,
            CreatedAtUtc = DateTime.UtcNow,
            LockoutEnabled = true
        };

        var creado = await userManager.CreateAsync(user, password);
        if (!creado.Succeeded)
        {
            throw new DomainException("No fue posible crear el usuario. Revise la contraseña.");
        }

        await userManager.AddToRoleAsync(user, rolCanonico);
        await auditoria.RegistrarAsync(adminId, "Crear", "Usuario", user.Id.ToString(), $"{correo} ({rolCanonico})", cancellationToken);
        return new UsuarioStaffInfo(user.Id, correo, user.IsActive, [rolCanonico]);
    }

    public async Task<UsuarioStaffInfo> ActualizarRolesAsync(
        Guid usuarioId,
        IReadOnlyList<string> roles,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new DomainException("El usuario no existe.");

        var actuales = await userManager.GetRolesAsync(user);
        var staffNuevo = RolesStaffAdministrables.Normalizar(roles);
        var staffActual = actuales
            .Where(rol => RolesStaffAdministrables.Nombres.Contains(rol))
            .ToList();
        var conservados = actuales
            .Where(rol => !RolesStaffAdministrables.Nombres.Contains(rol))
            .ToList();

        if (staffNuevo.Count == 0 && conservados.Count == 0)
        {
            throw new DomainException("El usuario debe conservar al menos un rol.");
        }

        var aQuitar = staffActual.Except(staffNuevo, StringComparer.Ordinal).ToList();
        var aAgregar = staffNuevo.Except(staffActual, StringComparer.Ordinal).ToList();

        if (aQuitar.Contains(RolNombres.Administrador, StringComparer.Ordinal))
        {
            if (usuarioId == adminId)
            {
                throw new DomainException("No puede quitarse el rol Administrador a sí mismo.");
            }

            var adminsActivos = (await userManager.GetUsersInRoleAsync(RolNombres.Administrador))
                .Count(item => item.IsActive && item.Id != usuarioId);
            if (adminsActivos == 0)
            {
                throw new DomainException("Debe quedar al menos un administrador activo.");
            }
        }

        if (aQuitar.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, aQuitar);
        }

        if (aAgregar.Count > 0)
        {
            await userManager.AddToRolesAsync(user, aAgregar);
        }

        var finales = await userManager.GetRolesAsync(user);
        await auditoria.RegistrarAsync(
            adminId,
            "Actualizar",
            "Usuario",
            usuarioId.ToString(),
            string.Join(", ", finales),
            cancellationToken);
        return new UsuarioStaffInfo(user.Id, user.Email ?? string.Empty, user.IsActive, finales.ToList());
    }
}
