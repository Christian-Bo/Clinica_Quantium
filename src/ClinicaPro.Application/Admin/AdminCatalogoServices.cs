using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Admin;

public sealed record CrearMedicoInput(
    string Email,
    string Password,
    string Nombres,
    string Apellidos,
    string? NumeroColegiado,
    string? Telefono);

public sealed record UsuarioStaffInfo(Guid UsuarioId, string Email, bool IsActive, IReadOnlyList<string> Roles);

public sealed record AdminMedicoInfo(
    Guid MedicoId,
    Guid UsuarioId,
    string Email,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string? NumeroColegiado,
    string? Telefono,
    bool IsActive);

public interface IAdminStaffService
{
    Task<Medico> CrearMedicoAsync(CrearMedicoInput input, Guid adminId, CancellationToken cancellationToken);
    Task<Medico> ActualizarMedicoAsync(
        Guid medicoId,
        string nombres,
        string apellidos,
        string? colegiado,
        string? telefono,
        bool isActive,
        Guid adminId,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<UsuarioStaffInfo>> ListarUsuariosAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<AdminMedicoInfo>> ListarMedicosAsync(CancellationToken cancellationToken);
    Task CambiarActivoUsuarioAsync(Guid usuarioId, bool isActive, Guid adminId, CancellationToken cancellationToken);
    Task<UsuarioStaffInfo> CrearUsuarioStaffAsync(
        string email,
        string password,
        string rol,
        Guid adminId,
        CancellationToken cancellationToken);
    Task<UsuarioStaffInfo> ActualizarRolesAsync(
        Guid usuarioId,
        IReadOnlyList<string> roles,
        Guid adminId,
        CancellationToken cancellationToken);
}

public sealed class AdministrarHorariosService(
    IMedicoRepository medicos,
    IHorarioRepository horarios,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public async Task<Horario> CrearAsync(
        Guid medicoId,
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly? vigenteDesde,
        DateOnly? vigenteHasta,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        _ = await medicos.ObtenerRastreadoAsync(medicoId, cancellationToken)
            ?? throw new DomainException("El médico no existe.");

        var horario = Horario.Create(medicoId, diaSemana, horaInicio, horaFin, vigenteDesde, vigenteHasta);
        await horarios.AgregarAsync(horario, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Crear", "Horario", horario.Id.ToString(), null, cancellationToken);
        return horario;
    }

    public async Task<Horario> ActualizarAsync(
        Guid medicoId,
        Guid horarioId,
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly? vigenteDesde,
        DateOnly? vigenteHasta,
        bool isActive,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var horario = await horarios.ObtenerRastreadoAsync(horarioId, cancellationToken)
            ?? throw new DomainException("El horario no existe.");

        if (horario.MedicoId != medicoId)
        {
            throw new DomainException("El horario no pertenece al médico indicado.");
        }

        horario.Actualizar(diaSemana, horaInicio, horaFin, vigenteDesde, vigenteHasta, isActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "Horario", horario.Id.ToString(), null, cancellationToken);
        return horario;
    }

    public async Task EliminarAsync(Guid horarioId, Guid adminId, CancellationToken cancellationToken)
    {
        var horario = await horarios.ObtenerRastreadoAsync(horarioId, cancellationToken)
            ?? throw new DomainException("El horario no existe.");

        horario.Desactivar();
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Desactivar", "Horario", horario.Id.ToString(), null, cancellationToken);
    }
}

public sealed class AdministrarParametrosService(
    IParametroRepository parametros,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public async Task<Parametro> ActualizarAsync(string clave, string valor, Guid adminId, CancellationToken cancellationToken)
    {
        if (string.Equals(clave, ParametrosClave.MaximoReprogramaciones, StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException("El máximo de reprogramaciones está fijo en 3 y no se puede editar.");
        }

        var parametro = await parametros.ObtenerRastreadoAsync(clave, cancellationToken)
            ?? throw new DomainException("El parámetro no existe.");

        parametro.CambiarValor(valor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "Parametro", clave, valor, cancellationToken);
        return parametro;
    }
}
