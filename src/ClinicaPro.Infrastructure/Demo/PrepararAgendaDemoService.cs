using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Infrastructure.Identity;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Demo;

public sealed class PrepararAgendaDemoService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    ClinicaProDbContext dbContext) : IPrepararAgendaDemo
{
    public static readonly Guid SecretariaUsuarioId = Guid.Parse("30000000-0000-0000-0000-000000000002");
    public static readonly Guid MedicoUsuarioId = Guid.Parse("30000000-0000-0000-0000-000000000003");
    public static readonly Guid MedicoId = Guid.Parse("40000000-0000-0000-0000-000000000001");
    public static readonly Guid Medico2UsuarioId = Guid.Parse("30000000-0000-0000-0000-000000000004");
    public static readonly Guid Medico2Id = Guid.Parse("40000000-0000-0000-0000-000000000002");

    public async Task<string> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        await AsegurarUsuarioConRolAsync(
            SecretariaUsuarioId,
            "secretaria@clinica.com",
            "Secretaria123!",
            RolNombres.Secretaria,
            cancellationToken);

        var medicoUsuario = await AsegurarUsuarioConRolAsync(
            MedicoUsuarioId,
            "medico@clinica.com",
            "Medico123!",
            RolNombres.Medico,
            cancellationToken);

        var medico2Usuario = await AsegurarUsuarioConRolAsync(
            Medico2UsuarioId,
            "medico2@clinica.com",
            "Medico123!",
            RolNombres.Medico,
            cancellationToken);

        try
        {
            await AsegurarMedicoAsync(
                MedicoId,
                medicoUsuario.Id,
                "Carlos",
                "Hernandez",
                "COL-1001",
                "55500011",
                cancellationToken);

            await AsegurarMedicoAsync(
                Medico2Id,
                medico2Usuario.Id,
                "Ana",
                "Morales",
                "COL-1002",
                "55500012",
                cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            throw SqlServerExceptionMapper.Map(exception);
        }

        return "Agenda lista: secretaria@clinica.com / Secretaria123!, medico@clinica.com y medico2@clinica.com / Medico123!. Lunes a viernes 08:00-16:00 (hora de Guatemala).";
    }

    private async Task AsegurarMedicoAsync(
        Guid medicoId,
        Guid usuarioId,
        string nombres,
        string apellidos,
        string colegiado,
        string telefono,
        CancellationToken cancellationToken)
    {
        if (!await dbContext.Medicos.AnyAsync(medico => medico.Id == medicoId, cancellationToken))
        {
            await dbContext.Medicos.AddAsync(
                Medico.Create(medicoId, usuarioId, nombres, apellidos, colegiado, telefono),
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!await dbContext.Horarios.AnyAsync(horario => horario.MedicoId == medicoId, cancellationToken))
        {
            for (byte dia = 1; dia <= 5; dia++)
            {
                await dbContext.Horarios.AddAsync(
                    Horario.Create(medicoId, dia, new TimeOnly(8, 0), new TimeOnly(16, 0)),
                    cancellationToken);
            }
        }
    }

    private async Task<ApplicationUser> AsegurarUsuarioConRolAsync(
        Guid usuarioId,
        string email,
        string password,
        string rol,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var existente = await userManager.FindByEmailAsync(email);
        if (existente is not null)
        {
            if (!await userManager.IsInRoleAsync(existente, rol))
            {
                await userManager.AddToRoleAsync(existente, rol);
            }

            return existente;
        }

        if (await roleManager.FindByNameAsync(rol) is null)
        {
            throw new InvalidOperationException($"No existe el rol {rol} en la base.");
        }

        var user = new ApplicationUser
        {
            Id = usuarioId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
            LockoutEnabled = true
        };

        var creado = await userManager.CreateAsync(user, password);
        if (!creado.Succeeded)
        {
            throw new InvalidOperationException(creado.Errors.First().Description);
        }

        await userManager.AddToRoleAsync(user, rol);
        return user;
    }
}