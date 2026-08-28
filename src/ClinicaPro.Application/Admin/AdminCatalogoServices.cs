using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
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
    string? Telefono,
    Guid EspecialidadId,
    bool EsPrimario);

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
    IReadOnlyList<Guid> EspecialidadIds,
    Guid? EspecialidadPrimariaId,
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
}

public sealed class AdministrarEspecialidadesService(
    IEspecialidadRepository especialidades,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public Task<IReadOnlyList<Especialidad>> ListarAsync(CancellationToken cancellationToken = default)
        => especialidades.ListarTodasAsync(cancellationToken);

    public async Task<Especialidad> CrearAsync(string nombre, string? descripcion, Guid adminId, CancellationToken cancellationToken)
    {
        var especialidad = Especialidad.Create(nombre, descripcion);
        await especialidades.AgregarAsync(especialidad, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Crear", "Especialidad", especialidad.Id.ToString(), especialidad.Nombre, cancellationToken);
        return especialidad;
    }

    public async Task<Especialidad> ActualizarAsync(
        Guid especialidadId,
        string nombre,
        string? descripcion,
        bool isActive,
        Guid adminId,
        CancellationToken cancellationToken)
    {
        var especialidad = await especialidades.ObtenerRastreadaAsync(especialidadId, cancellationToken)
            ?? throw new DomainException("La especialidad no existe.");

        especialidad.Actualizar(nombre, descripcion);
        especialidad.CambiarActivo(isActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "Especialidad", especialidad.Id.ToString(), especialidad.Nombre, cancellationToken);
        return especialidad;
    }
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
        Guid adminId,
        CancellationToken cancellationToken)
    {
        _ = await medicos.ObtenerRastreadoAsync(medicoId, cancellationToken)
            ?? throw new DomainException("El médico no existe.");

        var horario = Horario.Create(medicoId, diaSemana, horaInicio, horaFin);
        await horarios.AgregarAsync(horario, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Crear", "Horario", horario.Id.ToString(), null, cancellationToken);
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
        var parametro = await parametros.ObtenerRastreadoAsync(clave, cancellationToken)
            ?? throw new DomainException("El parámetro no existe.");

        parametro.CambiarValor(valor);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "Parametro", clave, valor, cancellationToken);
        return parametro;
    }
}
