using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Cita
{
    public const int MotivoMinLength = 5;
    public const int MotivoMaxLength = 500;
    public const int DuracionPredeterminadaMinutos = 30;

    public Guid Id { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid MedicoId { get; private set; }
    public Guid EspecialidadId { get; private set; }
    public DateTime FechaHoraInicio { get; private set; }
    public DateTime FechaHoraFin { get; private set; }
    public string MotivoConsulta { get; private set; } = null!;
    public string Estado { get; private set; } = null!;
    public byte NumeroReprogramaciones { get; private set; }
    public Guid? AutorizacionTerceraPorUsuarioId { get; private set; }
    public Guid CreadaPorUsuarioId { get; private set; }
    public Guid? SecretariaResponsableId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? UpdatedAtUtc { get; private set; }
    public byte[] RowVersion { get; private set; } = null!;

    private Cita()
    {
    }

    public static Cita Solicitar(
        Guid pacienteId,
        Guid medicoId,
        Guid especialidadId,
        Guid creadaPorUsuarioId,
        DateTime fechaHoraInicio,
        string motivoConsulta,
        int duracionMinutos = DuracionPredeterminadaMinutos)
    {
        var motivo = (motivoConsulta ?? string.Empty).Trim();
        if (motivo.Length < MotivoMinLength)
        {
            throw new DomainException("El motivo de consulta debe tener al menos 5 caracteres.");
        }

        if (motivo.Length > MotivoMaxLength)
        {
            throw new DomainException("El motivo de consulta no puede superar 500 caracteres.");
        }

        if (duracionMinutos <= 0)
        {
            throw new DomainException("La duración de la cita debe ser mayor a cero.");
        }

        var inicio = ComoHoraClinica(fechaHoraInicio);
        var fin = inicio.AddMinutes(duracionMinutos);
        if (inicio.Date != fin.Date)
        {
            throw new DomainException("La cita debe iniciar y terminar el mismo día.");
        }

        return new Cita
        {
            Id = Guid.NewGuid(),
            PacienteId = pacienteId,
            MedicoId = medicoId,
            EspecialidadId = especialidadId,
            FechaHoraInicio = inicio,
            FechaHoraFin = fin,
            MotivoConsulta = motivo,
            Estado = CitaEstados.Solicitada,
            CreadaPorUsuarioId = creadaPorUsuarioId,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void ConfirmarPorSecretaria(Guid secretariaUsuarioId)
    {
        ExigirEstado(CitaEstados.Solicitada, "Solo una cita Solicitada puede pasar a Programada.");
        AplicarEstado(CitaEstados.Programada, secretariaUsuarioId);
    }

    public void RechazarPorSecretaria(Guid secretariaUsuarioId)
    {
        ExigirEstado(CitaEstados.Solicitada, "Solo una cita Solicitada puede rechazarse.");
        AplicarEstado(CitaEstados.Rechazada, secretariaUsuarioId);
    }

    public void CancelarSolicitudPorPaciente()
    {
        ExigirEstado(CitaEstados.Solicitada, "Solo una solicitud pendiente puede anularse.");
        AplicarEstado(CitaEstados.Rechazada);
    }

    public void ConfirmarAsistencia()
    {
        ExigirEstado(CitaEstados.Programada, "Solo una cita Programada puede confirmarse.");
        AplicarEstado(CitaEstados.Confirmada);
    }

    public void Cancelar()
    {
        if (Estado is not (CitaEstados.Programada or CitaEstados.Confirmada))
        {
            throw new DomainException("Solo una cita Programada o Confirmada puede cancelarse.");
        }

        AplicarEstado(CitaEstados.Cancelada);
    }

    public void RegistrarLlegada()
    {
        ExigirEstado(CitaEstados.Confirmada, "Solo una cita Confirmada puede pasar a espera.");
        AplicarEstado(CitaEstados.EnEspera);
    }

    public void IniciarAtencion()
    {
        ExigirEstado(CitaEstados.EnEspera, "Solo una cita En Espera puede iniciar atención.");
        AplicarEstado(CitaEstados.EnAtencion);
    }

    public void FinalizarAtencion()
    {
        ExigirEstado(CitaEstados.EnAtencion, "Solo una cita En Atencion puede marcarse como Atendida.");
        AplicarEstado(CitaEstados.Atendida);
    }

    public void MarcarNoPresentada()
    {
        if (Estado is not (CitaEstados.Programada or CitaEstados.Confirmada or CitaEstados.EnEspera))
        {
            throw new DomainException("Solo una cita Programada, Confirmada o En Espera puede marcarse como no presentada.");
        }

        AplicarEstado(CitaEstados.NoPresentada);
    }

    private void ExigirEstado(string esperado, string mensaje)
    {
        if (Estado != esperado)
        {
            throw new DomainException(mensaje);
        }
    }

    private void AplicarEstado(string nuevoEstado, Guid? secretariaUsuarioId = null)
    {
        Estado = nuevoEstado;
        if (secretariaUsuarioId is not null)
        {
            SecretariaResponsableId = secretariaUsuarioId;
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    private static DateTime ComoHoraClinica(DateTime fecha)
    {
        var local = DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified);
        return new DateTime(
            local.Year,
            local.Month,
            local.Day,
            local.Hour,
            local.Minute,
            local.Second,
            DateTimeKind.Unspecified);
    }
}
