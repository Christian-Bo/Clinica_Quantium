using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Citas;

public sealed record SlotDisponible(DateTime FechaHoraInicio, DateTime FechaHoraFin, Guid MedicoId, string MedicoNombre);

public sealed class ListarDisponibilidadService(
    IMedicoRepository medicos,
    IHorarioRepository horarios,
    ICitaRepository citas,
    IParametroRepository parametros)
{
    public async Task<IReadOnlyList<SlotDisponible>> ExecuteAsync(
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var activos = await medicos.ListarActivosAsync(cancellationToken);
        if (activos.Count == 0)
        {
            return [];
        }

        var duracion = await parametros.ObtenerEnteroAsync(
            "Citas.DuracionPredeterminadaMinutos",
            Cita.DuracionPredeterminadaMinutos,
            cancellationToken);

        var dia = HoraClinica.DiaSemana(fecha.ToDateTime(TimeOnly.MinValue));
        var inicioDia = fecha.ToDateTime(TimeOnly.MinValue);
        var finDia = inicioDia.AddDays(1);
        var ahora = HoraClinica.Ahora();
        var slots = new List<SlotDisponible>();

        foreach (var medico in activos)
        {
            var jornada = (await horarios.ListarPorMedicoAsync(medico.Id, cancellationToken))
                .Where(horario =>
                    horario.DiaSemana == dia
                    && (horario.VigenteDesde is null || fecha >= horario.VigenteDesde)
                    && (horario.VigenteHasta is null || fecha <= horario.VigenteHasta))
                .ToList();

            if (jornada.Count == 0)
            {
                continue;
            }

            var ocupadas = await citas.ListarQueBloqueanEnRangoAsync(medico.Id, inicioDia, finDia, cancellationToken);

            foreach (var bloque in jornada.OrderBy(item => item.HoraInicio))
            {
                var cursor = fecha.ToDateTime(bloque.HoraInicio);
                var limite = fecha.ToDateTime(bloque.HoraFin);

                while (cursor.AddMinutes(duracion) <= limite)
                {
                    var finSlot = cursor.AddMinutes(duracion);
                    var choca = ocupadas.Any(cita =>
                        cita.FechaHoraInicio < finSlot && cursor < cita.FechaHoraFin);

                    if (!choca && cursor > ahora)
                    {
                        slots.Add(new SlotDisponible(cursor, finSlot, medico.Id, medico.NombreCompleto));
                    }

                    cursor = finSlot;
                }
            }
        }

        return slots
            .OrderBy(slot => slot.FechaHoraInicio)
            .ThenBy(slot => slot.MedicoNombre)
            .ToList();
    }
}