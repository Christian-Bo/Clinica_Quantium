using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Admin;
using ClinicaPro.Application.Pacientes;

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
    IPacienteRepository pacientes,
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

        if (string.IsNullOrWhiteSpace(input.NumeroColegiado))
        {
            throw new DomainException("El número de colegiado es obligatorio para registrar un médico.");
        }

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
        string? email,
        string? password,
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
            await AplicarIdentidadAsync(
                user,
                email,
                password,
                invalidarSesion: !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password) || !isActive);
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

        var usuarioIds = lista.Select(item => item.UsuarioId).ToList();
        var correos = await dbContext.Users.AsNoTracking()
            .Where(usuario => usuarioIds.Contains(usuario.Id))
            .Select(usuario => new { usuario.Id, usuario.Email })
            .ToListAsync(cancellationToken);
        var porUsuario = correos.ToDictionary(item => item.Id, item => item.Email ?? string.Empty);

        return lista.Select(medico => new AdminMedicoInfo(
            medico.Id,
            medico.UsuarioId,
            porUsuario.GetValueOrDefault(medico.UsuarioId, string.Empty),
            medico.Nombres,
            medico.Apellidos,
            medico.NombreCompleto,
            medico.NumeroColegiado,
            medico.Telefono,
            medico.IsActive)).ToList();
    }

    public Task CambiarActivoUsuarioAsync(Guid usuarioId, bool isActive, Guid adminId, CancellationToken cancellationToken)
        => ActualizarIdentidadUsuarioAsync(usuarioId, isActive, email: null, password: null, adminId, cancellationToken);

    public async Task ActualizarIdentidadUsuarioAsync(
        Guid usuarioId,
        bool isActive,
        string? email,
        string? password,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new DomainException("El usuario no existe.");

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains(RolNombres.Administrador) && !isActive)
        {
            throw new DomainException("No se puede desactivar un administrador.");
        }

        if (usuarioId == adminId && !isActive)
        {
            throw new DomainException("No puede desactivar su propia cuenta.");
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var cambioCredenciales = !string.IsNullOrWhiteSpace(email) || !string.IsNullOrWhiteSpace(password);
        await AplicarIdentidadAsync(user, email, password, invalidarSesion: cambioCredenciales || !isActive);
        await userManager.UpdateAsync(user);
        await auditoria.RegistrarAsync(
            adminId,
            isActive ? "Actualizar" : "Desactivar",
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
        string? tipoClinico,
        FichaMedicoAccesoInput? medico,
        FichaPacienteAccesoInput? paciente,
        CancellationToken cancellationToken)
    {
        var user = await userManager.FindByIdAsync(usuarioId.ToString())
            ?? throw new DomainException("El usuario no existe.");

        var actuales = await userManager.GetRolesAsync(user);
        var staffNuevo = RolesStaffAdministrables.Normalizar(roles);
        var tipoDestino = tipoClinico is null
            ? TipoClinicoUsuario.Inferir(actuales)
            : TipoClinicoUsuario.Normalizar(tipoClinico);
        MatrizCompatibilidadRoles.ExigirCompatible(staffNuevo, tipoDestino);

        var staffActual = actuales
            .Where(rol => RolesStaffAdministrables.Nombres.Contains(rol))
            .ToList();

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

        if (tipoClinico is not null)
        {
            await AplicarTipoClinicoAsync(
                user,
                actuales,
                TipoClinicoUsuario.Normalizar(tipoClinico),
                medico,
                paciente,
                cancellationToken);
        }

        actuales = await userManager.GetRolesAsync(user);
        var clinicos = actuales
            .Where(rol => !RolesStaffAdministrables.Nombres.Contains(rol))
            .ToList();

        if (staffNuevo.Count == 0 && clinicos.Count == 0)
        {
            throw new DomainException("El usuario debe conservar al menos un rol.");
        }

        if (aQuitar.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, aQuitar);
        }

        if (aAgregar.Count > 0)
        {
            await userManager.AddToRolesAsync(user, aAgregar);
        }

        await userManager.UpdateSecurityStampAsync(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);

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

    private async Task AplicarTipoClinicoAsync(
        ApplicationUser user,
        IList<string> rolesActuales,
        string tipoNuevo,
        FichaMedicoAccesoInput? fichaMedico,
        FichaPacienteAccesoInput? fichaPaciente,
        CancellationToken cancellationToken)
    {
        var esMedico = rolesActuales.Contains(RolNombres.Medico, StringComparer.OrdinalIgnoreCase);
        var esPaciente = rolesActuales.Contains(RolNombres.Paciente, StringComparer.OrdinalIgnoreCase);
        var quiereMedico = tipoNuevo == TipoClinicoUsuario.Medico;
        var quierePaciente = tipoNuevo == TipoClinicoUsuario.Paciente;

        if (esMedico && !quiereMedico)
        {
            await QuitarMedicoAsync(user.Id, cancellationToken);
        }

        if (esPaciente && !quierePaciente)
        {
            await QuitarPacienteAsync(user.Id, cancellationToken);
        }

        if (quiereMedico && !esMedico)
        {
            await AsignarMedicoAsync(user, fichaMedico, cancellationToken);
        }

        if (quierePaciente && !esPaciente)
        {
            await AsignarPacienteAsync(user, fichaPaciente, cancellationToken);
        }
    }

    private async Task QuitarMedicoAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var medico = await dbContext.Medicos.FirstOrDefaultAsync(
            item => item.UsuarioId == usuarioId,
            cancellationToken);
        if (medico is null)
        {
            var user = await userManager.FindByIdAsync(usuarioId.ToString());
            if (user is not null && await userManager.IsInRoleAsync(user, RolNombres.Medico))
            {
                await userManager.RemoveFromRoleAsync(user, RolNombres.Medico);
            }

            return;
        }

        await ExigirSinCitasActivasAsync(
            medicoId: medico.Id,
            pacienteId: null,
            "Cancele o reprograme las citas activas de este médico antes de quitarle el rol.",
            cancellationToken);

        medico.CambiarActivo(false);
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is not null && await userManager.IsInRoleAsync(usuario, RolNombres.Medico))
        {
            await userManager.RemoveFromRoleAsync(usuario, RolNombres.Medico);
        }
    }

    private async Task QuitarPacienteAsync(Guid usuarioId, CancellationToken cancellationToken)
    {
        var paciente = await pacientes.ObtenerRastreadoPorUsuarioIdAsync(usuarioId, cancellationToken);
        if (paciente is null)
        {
            var user = await userManager.FindByIdAsync(usuarioId.ToString());
            if (user is not null && await userManager.IsInRoleAsync(user, RolNombres.Paciente))
            {
                await userManager.RemoveFromRoleAsync(user, RolNombres.Paciente);
            }

            return;
        }

        await ExigirSinCitasActivasAsync(
            medicoId: null,
            pacienteId: paciente.Id,
            "Cancele o reprograme las citas activas de este paciente antes de quitarle el rol.",
            cancellationToken);

        paciente.CambiarActivo(false);
        var usuario = await userManager.FindByIdAsync(usuarioId.ToString());
        if (usuario is not null && await userManager.IsInRoleAsync(usuario, RolNombres.Paciente))
        {
            await userManager.RemoveFromRoleAsync(usuario, RolNombres.Paciente);
        }
    }

    private async Task AsignarMedicoAsync(
        ApplicationUser user,
        FichaMedicoAccesoInput? ficha,
        CancellationToken cancellationToken)
    {
        var existente = await dbContext.Medicos.FirstOrDefaultAsync(
            item => item.UsuarioId == user.Id,
            cancellationToken);
        if (existente is null)
        {
            if (ficha is null
                || string.IsNullOrWhiteSpace(ficha.Nombres)
                || string.IsNullOrWhiteSpace(ficha.Apellidos)
                || string.IsNullOrWhiteSpace(ficha.NumeroColegiado))
            {
                throw new DomainException("Para asignar el rol Médico indique nombres, apellidos y colegiado.");
            }

            var medico = Medico.Create(
                Guid.NewGuid(),
                user.Id,
                ficha.Nombres,
                ficha.Apellidos,
                ficha.NumeroColegiado,
                ficha.Telefono);
            await medicos.AgregarAsync(medico, cancellationToken);
        }
        else
        {
            existente.CambiarActivo(true);
            if (ficha is not null)
            {
                existente.Actualizar(ficha.Nombres, ficha.Apellidos, ficha.NumeroColegiado, ficha.Telefono);
            }
        }

        if (!await userManager.IsInRoleAsync(user, RolNombres.Medico))
        {
            await userManager.AddToRoleAsync(user, RolNombres.Medico);
        }
    }

    private async Task AsignarPacienteAsync(
        ApplicationUser user,
        FichaPacienteAccesoInput? ficha,
        CancellationToken cancellationToken)
    {
        var existente = await pacientes.ObtenerRastreadoPorUsuarioIdAsync(user.Id, cancellationToken);
        if (existente is null)
        {
            if (ficha is null
                || string.IsNullOrWhiteSpace(ficha.Nombres)
                || string.IsNullOrWhiteSpace(ficha.Apellidos)
                || string.IsNullOrWhiteSpace(ficha.Documento))
            {
                throw new DomainException("Para asignar el rol Paciente indique nombres, apellidos y DPI.");
            }

            if (await pacientes.ExisteDocumentoAsync(ficha.Documento.Trim(), exceptoPacienteId: null, cancellationToken))
            {
                throw new DomainException("Ya existe un paciente con ese documento.");
            }

            var paciente = Paciente.Create(
                user.Id,
                ficha.Nombres,
                ficha.Apellidos,
                ficha.Documento,
                ficha.FechaNacimiento,
                ficha.Telefono);
            await pacientes.AgregarAsync(paciente, cancellationToken);
        }
        else
        {
            existente.CambiarActivo(true);
            if (ficha is not null)
            {
                existente.Actualizar(
                    ficha.Nombres,
                    ficha.Apellidos,
                    ficha.Documento,
                    ficha.FechaNacimiento,
                    ficha.Telefono,
                    direccion: null);
            }
        }

        if (!await userManager.IsInRoleAsync(user, RolNombres.Paciente))
        {
            await userManager.AddToRoleAsync(user, RolNombres.Paciente);
        }
    }

    private async Task ExigirSinCitasActivasAsync(
        Guid? medicoId,
        Guid? pacienteId,
        string mensaje,
        CancellationToken cancellationToken)
    {
        var ahora = HoraClinica.Ahora();
        var bloquean = new[]
        {
            CitaEstados.Solicitada,
            CitaEstados.Programada,
            CitaEstados.Confirmada,
            CitaEstados.EnEspera,
            CitaEstados.EnAtencion
        };

        var hay = await dbContext.Citas.AnyAsync(
            cita => bloquean.Contains(cita.Estado)
                && cita.FechaHoraInicio >= ahora
                && ((medicoId != null && cita.MedicoId == medicoId)
                    || (pacienteId != null && cita.PacienteId == pacienteId)),
            cancellationToken);

        if (hay)
        {
            throw new DomainException(mensaje);
        }
    }

    private async Task AplicarIdentidadAsync(
        ApplicationUser user,
        string? email,
        string? password,
        bool invalidarSesion)
    {
        if (!string.IsNullOrWhiteSpace(email))
        {
            var correo = email.Trim();
            var otro = await userManager.FindByEmailAsync(correo);
            if (otro is not null && otro.Id != user.Id)
            {
                throw new DomainException("El correo ya está registrado.");
            }

            var emailResultado = await userManager.SetEmailAsync(user, correo);
            if (!emailResultado.Succeeded)
            {
                throw new DomainException("No fue posible actualizar el correo.");
            }

            var userNameResultado = await userManager.SetUserNameAsync(user, correo);
            if (!userNameResultado.Succeeded)
            {
                throw new DomainException("No fue posible actualizar el correo.");
            }
        }

        if (!string.IsNullOrWhiteSpace(password))
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var reset = await userManager.ResetPasswordAsync(user, token, password);
            if (!reset.Succeeded)
            {
                throw new DomainException("La contraseña no cumple la política de seguridad.");
            }

            user.MustChangePassword = true;
            user.UpdatedAtUtc = DateTime.UtcNow;
        }

        if (invalidarSesion)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
    }
}
