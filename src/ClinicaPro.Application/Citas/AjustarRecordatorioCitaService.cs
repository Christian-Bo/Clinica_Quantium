using ClinicaPro.Application.Notificaciones;
using ClinicaPro.Domain;

namespace ClinicaPro.Application.Citas;

public sealed class AjustarRecordatorioCitaService(INotificacionRepository notificaciones)
{
    public Task AnularPendientesAsync(Guid citaId, string motivo, CancellationToken cancellationToken = default)
    {
        return notificaciones.AnularPendientesDeTipoAsync(
            citaId,
            NotificacionTipos.RecordatorioCita,
            motivo,
            cancellationToken);
    }
}
