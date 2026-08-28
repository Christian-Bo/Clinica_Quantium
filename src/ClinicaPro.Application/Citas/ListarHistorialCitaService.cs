using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class ListarHistorialCitaService(
    ICitaRepository citas,
    IHistorialCitaRepository historial,
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    IActorConsulta actores)
{
    public async Task<IReadOnlyList<HistorialCitaExplicativo>> ExecuteAsync(
        Guid citaId,
        Guid usuarioId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default)
    {
        var cita = await citas.ObtenerPorIdAsync(citaId, cancellationToken)
            ?? throw new DomainException("La cita no existe.");

        await AsegurarAccesoAsync(cita, usuarioId, roles, cancellationToken);

        var items = await historial.ListarPorCitaAsync(citaId, cancellationToken);
        var ids = items.Select(item => item.UsuarioId).Distinct().ToList();
        var mapa = await actores.ObtenerPorIdsAsync(ids, cancellationToken);

        return items.Select(item =>
        {
            mapa.TryGetValue(item.UsuarioId, out var actor);
            var nombre = actor?.Nombre ?? "Usuario del sistema";
            var rol = actor?.Rol ?? "Usuario";
            return new HistorialCitaExplicativo(
                item.Id,
                item.CitaId,
                item.UsuarioId,
                nombre,
                rol,
                item.TipoCambio,
                item.EstadoAnterior,
                item.EstadoNuevo,
                item.FechaHoraInicioAnterior,
                item.FechaHoraInicioNueva,
                item.FechaHoraFinAnterior,
                item.FechaHoraFinNueva,
                item.Motivo,
                item.FechaCambioUtc,
                HoraClinica.ALocal(item.FechaCambioUtc),
                HistorialCitaNarrativa.Redactar(
                    item.TipoCambio,
                    nombre,
                    rol,
                    item.EstadoAnterior,
                    item.EstadoNuevo,
                    item.FechaHoraInicioAnterior,
                    item.FechaHoraInicioNueva,
                    item.FechaHoraFinAnterior,
                    item.FechaHoraFinNueva,
                    item.Motivo));
        }).ToList();
    }

    private async Task AsegurarAccesoAsync(
        Cita cita,
        Guid usuarioId,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        if (roles.Contains(RolNombres.Administrador) || roles.Contains(RolNombres.Secretaria))
        {
            return;
        }

        if (roles.Contains(RolNombres.Paciente))
        {
            var paciente = await pacientes.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);
            if (paciente is not null && paciente.Id == cita.PacienteId)
            {
                return;
            }
        }

        if (roles.Contains(RolNombres.Medico))
        {
            var medico = await medicos.ObtenerPorUsuarioIdAsync(usuarioId, cancellationToken);
            if (medico is not null && medico.Id == cita.MedicoId)
            {
                return;
            }
        }

        throw new DomainException("No tiene permiso para consultar el historial de esta cita.");
    }
}
