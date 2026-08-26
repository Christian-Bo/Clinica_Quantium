using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed record SlotDisponible(DateTime FechaHoraInicio, DateTime FechaHoraFin, Guid MedicoId, string MedicoNombre);

public sealed class ListarDisponibilidadService(
    IMedicoRepository medicos,
    IHorarioRepository horarios,
    ICitaRepository citas,
    IParametroRepository parametros)
{
    public async Task<IReadOnlyList<SlotDisponible>> ExecuteAsync(
        Guid especialidadId,
        DateOnly fecha,
        CancellationToken cancellationToken = default)
    {
        var medico = await medicos.ObtenerPrimarioPorEspecialidadAsync(especialidadId, cancellationToken)
            ?? throw new DomainException("La especialidad no tiene un médico primario activo.");

        var duracion = await parametros.ObtenerEnteroAsync(
            "Citas.DuracionPredeterminadaMinutos",
            Cita.DuracionPredeterminadaMinutos,
            cancellationToken);

        var dia = HoraClinica.DiaSemana(fecha.ToDateTime(TimeOnly.MinValue));
        var jornada = (await horarios.ListarPorMedicoAsync(medico.Id, cancellationToken))
            .Where(horario =>
                horario.DiaSemana == dia
                && (horario.VigenteDesde is null || fecha >= horario.VigenteDesde)
                && (horario.VigenteHasta is null || fecha <= horario.VigenteHasta))
            .ToList();

        if (jornada.Count == 0)
        {
            return [];
        }

        var inicioDia = fecha.ToDateTime(TimeOnly.MinValue);
        var finDia = inicioDia.AddDays(1);
        var ocupadas = await citas.ListarQueBloqueanEnRangoAsync(medico.Id, inicioDia, finDia, cancellationToken);
        var ahora = HoraClinica.Ahora();
        var slots = new List<SlotDisponible>();

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

        return slots;
    }
}
