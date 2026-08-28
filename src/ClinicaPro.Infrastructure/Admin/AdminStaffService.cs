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
        var medicosLista = await medicos.ListarTodosAsync(cancellationToken);
        var especialidades = await medicos.ListarEspecialidadesActivasAsync(cancellationToken);
        var usuarios = await dbContext.Users.AsNoTracking().ToListAsync(cancellationToken);
        var emailPorUsuario = usuarios.ToDictionary(item => item.Id, item => item.Email ?? string.Empty);

        return medicosLista.Select(medico =>
        {
            var delMedico = especialidades.Where(relacion => relacion.MedicoId == medico.Id).ToList();
            return new AdminMedicoInfo(
                medico.Id,
                medico.UsuarioId,
                emailPorUsuario.GetValueOrDefault(medico.UsuarioId, string.Empty),
                medico.Nombres,
                medico.Apellidos,
                medico.NombreCompleto,
                medico.NumeroColegiado,
                medico.Telefono,
                delMedico.Select(relacion => relacion.EspecialidadId).ToList(),
                delMedico.FirstOrDefault(relacion => relacion.EsPrimario)?.EspecialidadId,
                medico.IsActive);
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
}
