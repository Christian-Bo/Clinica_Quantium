using ClinicaPro.Application;
using ClinicaPro.Application.Auth;
using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;
using ClinicaPro.Infrastructure.Identity;
using ClinicaPro.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClinicaPro.Infrastructure.Auth;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IPacienteRepository pacienteRepository,
    ClinicaProDbContext dbContext,
    JwtTokenGenerator jwtTokenGenerator,
    IEmailSender emailSender,
    IAuditoriaWriter auditoria) : IAuthService
{
    public async Task<AuthOperationResult> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());

        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Fail("invalid_credentials");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthOperationResult.Fail("locked_out");
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthOperationResult.Fail("invalid_credentials");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await ObtenerRolesActivosAsync(user, cancellationToken);
        var paciente = await pacienteRepository.ObtenerPorUsuarioIdAsync(user.Id, cancellationToken);
        var (token, expiresAt) = jwtTokenGenerator.Create(user, roles);

        await auditoria.RegistrarAsync(user.Id, "Login", "Usuario", user.Id.ToString(), user.Email, cancellationToken);

        return AuthOperationResult.Ok(new AuthSession(
            token,
            expiresAt,
            user.Id,
            user.Email ?? email,
            roles,
            user.MustChangePassword,
            paciente?.Id));
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(
        Guid usuarioId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Fail("invalid_credentials");
        }

        if (string.Equals(currentPassword, newPassword, StringComparison.Ordinal))
        {
            return AuthOperationResult.Fail("password_same");
        }

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        if (!changed.Succeeded)
        {
            var code = changed.Errors.FirstOrDefault()?.Code ?? "invalid_user";
            return AuthOperationResult.Fail(code == "PasswordMismatch" ? "password_mismatch" : code);
        }

        if (user.MustChangePassword)
        {
            user.MustChangePassword = false;
            user.UpdatedAtUtc = DateTime.UtcNow;
            await userManager.UpdateAsync(user);
        }

        var roles = await ObtenerRolesActivosAsync(user, cancellationToken);
        var paciente = await pacienteRepository.ObtenerPorUsuarioIdAsync(user.Id, cancellationToken);
        var (token, expiresAt) = jwtTokenGenerator.Create(user, roles);

        return AuthOperationResult.Ok(new AuthSession(
            token,
            expiresAt,
            user.Id,
            user.Email ?? string.Empty,
            roles,
            false,
            paciente?.Id));
    }

    public async Task<AuthOperationResult> RegisterPacienteAsync(
        RegisterPacienteInput input,
        CancellationToken cancellationToken = default)
    {
        var email = input.Email.Trim();

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AuthOperationResult.Fail("email_taken");
        }

        if (!string.IsNullOrWhiteSpace(input.Documento)
            && await pacienteRepository.ExisteDocumentoAsync(input.Documento, null, cancellationToken))
        {
            return AuthOperationResult.Fail("documento_taken");
        }

        var rolPaciente = await roleManager.FindByNameAsync(RolNombres.Paciente);
        if (rolPaciente is null || !rolPaciente.IsActive)
        {
            return AuthOperationResult.Fail("role_missing");
        }

        Paciente paciente;

        try
        {
            paciente = Paciente.Create(
                Guid.NewGuid(),
                input.Nombres,
                input.Apellidos,
                input.Documento,
                input.FechaNacimiento,
                input.Telefono,
                input.Direccion);
        }
        catch (DomainException)
        {
            return AuthOperationResult.Fail("invalid_patient");
        }

        var user = new ApplicationUser
        {
            Id = paciente.UsuarioId,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            PhoneNumber = input.Telefono?.Trim(),
            IsActive = true,
            MustChangePassword = false,
            CreatedAtUtc = DateTime.UtcNow,
            LockoutEnabled = true
        };

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            var createResult = await userManager.CreateAsync(user, input.Password);
            if (!createResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AuthOperationResult.Fail(createResult.Errors.FirstOrDefault()?.Code ?? "invalid_user");
            }

            var roleResult = await userManager.AddToRoleAsync(user, RolNombres.Paciente);
            if (!roleResult.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return AuthOperationResult.Fail("role_assignment_failed");
            }

            await pacienteRepository.AgregarAsync(paciente, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            await auditoria.RegistrarAsync(user.Id, "RegistroPaciente", "Paciente", paciente.Id.ToString(), email, cancellationToken);

            var roles = new[] { RolNombres.Paciente };
            var (token, expiresAt) = jwtTokenGenerator.Create(user, roles);

            return AuthOperationResult.Ok(new AuthSession(
                token,
                expiresAt,
                user.Id,
                user.Email ?? email,
                roles,
                false,
                paciente.Id));
        });
    }

    public async Task<PacienteStaffResult> RegisterPacientePorStaffAsync(
        RegisterPacienteInput input,
        CancellationToken cancellationToken = default)
    {
        var resultado = await RegisterPacienteAsync(input, cancellationToken);
        if (!resultado.Succeeded || resultado.Session is null)
        {
            return new PacienteStaffResult(false, resultado.ErrorCode, null, null);
        }

        var user = await userManager.FindByIdAsync(resultado.Session.UsuarioId.ToString());
        if (user is not null)
        {
            user.MustChangePassword = true;
            await userManager.UpdateAsync(user);
        }

        return new PacienteStaffResult(
            true,
            null,
            resultado.Session.PacienteId,
            resultado.Session.UsuarioId);
    }

    public async Task<AuthUserInfo?> ObtenerUsuarioAsync(
        Guid usuarioId,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString());
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var roles = await ObtenerRolesActivosAsync(user, cancellationToken);
        var paciente = await pacienteRepository.ObtenerPorUsuarioIdAsync(user.Id, cancellationToken);

        return new AuthUserInfo(
            user.Id,
            user.Email ?? string.Empty,
            roles,
            user.MustChangePassword,
            paciente?.Id,
            paciente?.NombreCompleto);
    }

    private async Task<IReadOnlyList<string>> ObtenerRolesActivosAsync(
        ApplicationUser user,
        CancellationToken cancellationToken)
    {
        var roleNames = await userManager.GetRolesAsync(user);
        if (roleNames.Count == 0)
        {
            return [];
        }

        var normalized = roleNames.Select(nombre => nombre.ToUpperInvariant()).ToList();

        return await roleManager.Roles
            .AsNoTracking()
            .Where(rol => rol.IsActive && rol.NormalizedName != null && normalized.Contains(rol.NormalizedName))
            .Select(rol => rol.Name!)
            .ToListAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
        {
            return;
        }

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        await emailSender.SendAsync(
            user.Email ?? email,
            "Clínica Pro — restablecer contraseña",
            $"Hola,\n\nPara restablecer su contraseña use este código en la aplicación:\n\n{token}\n\nSi usted no lo pidió, ignore este correo.",
            cancellationToken);
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByEmailAsync(email.Trim());
        if (user is null || !user.IsActive)
        {
            return AuthOperationResult.Fail("invalid_token");
        }

        var resultado = await userManager.ResetPasswordAsync(user, token.Trim(), newPassword);
        if (!resultado.Succeeded)
        {
            return AuthOperationResult.Fail(resultado.Errors.FirstOrDefault()?.Code ?? "invalid_token");
        }

        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await userManager.UpdateAsync(user);
        await auditoria.RegistrarAsync(user.Id, "ResetPassword", "Usuario", user.Id.ToString(), user.Email, cancellationToken);
        return AuthOperationResult.Ok(new AuthSession(
            string.Empty,
            DateTimeOffset.UtcNow,
            user.Id,
            user.Email ?? email,
            [],
            false,
            null));
    }
}
