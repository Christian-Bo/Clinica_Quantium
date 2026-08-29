using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Admin;

public sealed record MedicoEspecialidadDetalle(
    Guid EspecialidadId,
    string Nombre,
    bool EsPrimario,
    bool IsActive);

public sealed class AdministrarMedicoEspecialidadesService(
    IMedicoRepository medicos,
    IEspecialidadRepository especialidades,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public async Task<IReadOnlyList<MedicoEspecialidadDetalle>> ListarAsync(
        Guid medicoId,
        CancellationToken cancellationToken = default)
    {
        _ = await medicos.ObtenerRastreadoAsync(medicoId, cancellationToken)
            ?? throw new DomainException("El médico no existe.");

        var relaciones = await medicos.ListarEspecialidadesDeMedicoAsync(medicoId, cancellationToken);
        var catalogo = (await especialidades.ListarTodasAsync(cancellationToken))
            .ToDictionary(item => item.Id, item => item.Nombre);

        return relaciones
            .OrderByDescending(item => item.EsPrimario)
            .ThenBy(item => catalogo.GetValueOrDefault(item.EspecialidadId, string.Empty))
            .Select(item => new MedicoEspecialidadDetalle(
                item.EspecialidadId,
                catalogo.GetValueOrDefault(item.EspecialidadId, "Especialidad"),
                item.EsPrimario,
                item.IsActive))
            .ToList();
    }

    public async Task<MedicoEspecialidadDetalle> AgregarAsync(
        Guid medicoId,
        Guid especialidadId,
        bool esPrimario,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        _ = await medicos.ObtenerRastreadoAsync(medicoId, cancellationToken)
            ?? throw new DomainException("El médico no existe.");

        var especialidad = await especialidades.ObtenerPorIdAsync(especialidadId, cancellationToken)
            ?? throw new DomainException("La especialidad no existe o no está activa.");

        if (esPrimario && await medicos.ExisteOtroPrimarioActivoAsync(especialidadId, medicoId, cancellationToken))
        {
            throw new DomainException("Ya existe un médico primario activo para esta especialidad.");
        }

        var existente = await medicos.ObtenerEspecialidadRastreadaAsync(medicoId, especialidadId, cancellationToken);
        if (existente is not null)
        {
            if (existente.IsActive)
            {
                throw new DomainException("El médico ya tiene esta especialidad.");
            }

            existente.CambiarActivo(true);
            existente.MarcarPrimario(esPrimario);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            await auditoria.RegistrarAsync(adminId, "Actualizar", "MedicoEspecialidad", $"{medicoId}:{especialidadId}", especialidad.Nombre, cancellationToken);
            return new MedicoEspecialidadDetalle(especialidad.Id, especialidad.Nombre, existente.EsPrimario, existente.IsActive);
        }

        var relacion = MedicoEspecialidad.Create(medicoId, especialidadId, esPrimario);
        await medicos.AgregarEspecialidadAsync(relacion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Crear", "MedicoEspecialidad", $"{medicoId}:{especialidadId}", especialidad.Nombre, cancellationToken);
        return new MedicoEspecialidadDetalle(especialidad.Id, especialidad.Nombre, relacion.EsPrimario, relacion.IsActive);
    }

    public async Task<MedicoEspecialidadDetalle> ActualizarAsync(
        Guid medicoId,
        Guid especialidadId,
        bool esPrimario,
        bool isActive,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var relacion = await medicos.ObtenerEspecialidadRastreadaAsync(medicoId, especialidadId, cancellationToken)
            ?? throw new DomainException("El médico no tiene esa especialidad.");

        if (esPrimario && isActive && await medicos.ExisteOtroPrimarioActivoAsync(especialidadId, medicoId, cancellationToken))
        {
            throw new DomainException("Ya existe un médico primario activo para esta especialidad.");
        }

        relacion.MarcarPrimario(esPrimario);
        relacion.CambiarActivo(isActive);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Actualizar", "MedicoEspecialidad", $"{medicoId}:{especialidadId}", null, cancellationToken);

        var especialidad = await especialidades.ObtenerRastreadaAsync(especialidadId, cancellationToken);
        return new MedicoEspecialidadDetalle(
            especialidadId,
            especialidad?.Nombre ?? "Especialidad",
            relacion.EsPrimario,
            relacion.IsActive);
    }

    public async Task QuitarAsync(
        Guid medicoId,
        Guid especialidadId,
        Guid adminId,
        CancellationToken cancellationToken = default)
    {
        var relacion = await medicos.ObtenerEspecialidadRastreadaAsync(medicoId, especialidadId, cancellationToken)
            ?? throw new DomainException("El médico no tiene esa especialidad.");

        relacion.CambiarActivo(false);
        relacion.MarcarPrimario(false);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(adminId, "Desactivar", "MedicoEspecialidad", $"{medicoId}:{especialidadId}", null, cancellationToken);
    }
}
