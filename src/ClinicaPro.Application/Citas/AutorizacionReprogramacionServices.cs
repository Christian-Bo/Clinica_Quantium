using ClinicaPro.Application;
using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class SolicitarAutorizacionReprogramacionService(
    ICitaRepository citas,
    IAutorizacionReprogramacionRepository autorizaciones,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public async Task<AutorizacionReprogramacion> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        if (cita.NumeroReprogramaciones != Cita.MaximoReprogramaciones - 1)
        {
            throw new DomainException("Solo la tercera reprogramación requiere autorización del Administrador.");
        }

        if (cita.Estado is not (CitaEstados.Solicitada or CitaEstados.Programada or CitaEstados.Confirmada))
        {
            throw new DomainException("Solo una cita Solicitada, Programada o Confirmada puede reprogramarse.");
        }

        if (await autorizaciones.ObtenerPendientePorCitaAsync(citaId, cancellationToken) is not null)
        {
            throw new DomainException("Ya existe una solicitud pendiente para esta cita.");
        }

        if (await autorizaciones.ObtenerAprobadaPorCitaAsync(citaId, cancellationToken) is not null)
        {
            throw new DomainException("Esta cita ya tiene una autorización aprobada.");
        }

        var autorizacion = AutorizacionReprogramacion.Solicitar(citaId, usuarioId, motivo);
        await autorizaciones.AgregarAsync(autorizacion, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(usuarioId, "Solicitar", "AutorizacionReprogramacion", autorizacion.Id.ToString(), motivo, cancellationToken);
        return autorizacion;
    }
}

public sealed class ListarAutorizacionesReprogramacionService(IAutorizacionReprogramacionRepository autorizaciones)
{
    public Task<IReadOnlyList<AutorizacionReprogramacion>> ExecuteAsync(
        string? estado,
        CancellationToken cancellationToken = default)
        => autorizaciones.ListarAsync(estado, cancellationToken);
}

public sealed class ResolverAutorizacionReprogramacionService(
    IAutorizacionReprogramacionRepository autorizaciones,
    IHistorialCitaRepository historial,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public Task<AutorizacionReprogramacion> AprobarAsync(
        Guid autorizacionId,
        Guid adminId,
        string? motivo,
        CancellationToken cancellationToken = default)
        => ResolverAsync(autorizacionId, adminId, aprobar: true, motivo, cancellationToken);

    public Task<AutorizacionReprogramacion> RechazarAsync(
        Guid autorizacionId,
        Guid adminId,
        string? motivo,
        CancellationToken cancellationToken = default)
        => ResolverAsync(autorizacionId, adminId, aprobar: false, motivo, cancellationToken);

    private async Task<AutorizacionReprogramacion> ResolverAsync(
        Guid autorizacionId,
        Guid adminId,
        bool aprobar,
        string? motivo,
        CancellationToken cancellationToken)
    {
        var autorizacion = await autorizaciones.ObtenerPorIdAsync(autorizacionId, cancellationToken)
            ?? throw new DomainException("La autorización no existe.");

        if (aprobar)
        {
            autorizacion.Aprobar(adminId, motivo);
        }
        else
        {
            autorizacion.Rechazar(adminId, motivo);
        }

        await historial.AgregarAsync(
            HistorialCita.RegistrarAutorizacion(
                autorizacion.CitaId,
                adminId,
                aprobar
                    ? $"Administrador aprobó la tercera reprogramación. {autorizacion.MotivoDecision}"
                    : $"Administrador rechazó la tercera reprogramación. {autorizacion.MotivoDecision}"),
            cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(
            adminId,
            aprobar ? "Aprobar" : "Rechazar",
            "AutorizacionReprogramacion",
            autorizacion.Id.ToString(),
            autorizacion.MotivoDecision,
            cancellationToken);
        return autorizacion;
    }
}
