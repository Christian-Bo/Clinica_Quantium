using ClinicaPro.Application.Agenda;
using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public sealed class ValidarAgendaPacienteService(ICitaRepository citas, IParametroRepository parametros)
{
    public async Task ExigirPuedeAgendarAsync(
        Guid pacienteId,
        DateTime fechaHoraInicio,
        DateTime fechaHoraFin,
        Guid? exceptoCitaId,
        bool cuentaComoCitaNueva,
        CancellationToken cancellationToken = default)
    {
        var solape = await citas.ListarQueBloqueanPacienteEnRangoAsync(
            pacienteId,
            fechaHoraInicio,
            fechaHoraFin,
            exceptoCitaId,
            cancellationToken);

        if (solape.Count > 0)
        {
            throw new DomainException(
                "Ya tiene una cita en ese horario. Cancele o elija otro horario.");
        }

        if (!cuentaComoCitaNueva)
        {
            return;
        }

        var maximo = await parametros.ObtenerEnteroAsync(
            ParametrosClave.MaximoActivasPorPaciente,
            Cita.MaximoActivasFuturasPorPaciente,
            cancellationToken);

        if (maximo < 1)
        {
            maximo = Cita.MaximoActivasFuturasPorPaciente;
        }

        var activas = await citas.ContarActivasFuturasAsync(
            pacienteId,
            HoraClinica.Ahora(),
            exceptoCitaId,
            cancellationToken);

        if (activas >= maximo)
        {
            throw new DomainException(
                $"No puede tener más de {maximo} citas activas. Cancele una o asista a las pendientes.");
        }
    }
}
