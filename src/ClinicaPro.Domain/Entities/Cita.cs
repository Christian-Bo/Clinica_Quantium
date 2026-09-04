using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Cita
{
    public const int MotivoMinLength = 5;
    public const int MotivoMaxLength = 500;
    public const int DuracionPredeterminadaMinutos = 30;
    public const int MaximoReprogramaciones = 3;

    public Guid Id { get; private set; }
    public Guid PacienteId { get; private set; }
    public Guid MedicoId { get; private set; }
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
        Cancelar(HoraClinica.Ahora(), horasMinimasAnticipacion: 2);
    }

    public void Cancelar(DateTime ahoraClinica, int horasMinimasAnticipacion)
    {
        if (Estado is not (CitaEstados.Programada or CitaEstados.Confirmada))
        {
            throw new DomainException("Solo una cita Programada o Confirmada puede cancelarse.");
        }

        var horas = horasMinimasAnticipacion < 0 ? 0 : horasMinimasAnticipacion;
        var conAnticipacion = ahoraClinica <= FechaHoraInicio.AddHours(-horas);
        AplicarEstado(conAnticipacion ? CitaEstados.Cancelada : CitaEstados.NoPresentada);
    }

    public void Reprogramar(
        DateTime nuevaFechaHoraInicio,
        int duracionMinutos,
        Guid? autorizacionAdministradorUsuarioId)
    {
        if (Estado is not (CitaEstados.Solicitada or CitaEstados.Programada or CitaEstados.Confirmada))
        {
            throw new DomainException("Solo una cita Solicitada, Programada o Confirmada puede reprogramarse.");
        }

        if (NumeroReprogramaciones >= MaximoReprogramaciones)
        {
            throw new DomainException("La cita ya alcanzó el máximo de reprogramaciones.");
        }

        if (duracionMinutos <= 0)
        {
            throw new DomainException("La duración de la cita debe ser mayor a cero.");
        }

        var inicio = ComoHoraClinica(nuevaFechaHoraInicio);
        var fin = inicio.AddMinutes(duracionMinutos);
        if (inicio.Date != fin.Date)
        {
            throw new DomainException("La cita debe iniciar y terminar el mismo día.");
        }

        if (inicio == FechaHoraInicio)
        {
            throw new DomainException("La nueva fecha debe ser distinta a la actual.");
        }

        var siguiente = (byte)(NumeroReprogramaciones + 1);
        if (siguiente >= MaximoReprogramaciones)
        {
            if (autorizacionAdministradorUsuarioId is null || autorizacionAdministradorUsuarioId == Guid.Empty)
            {
                throw new DomainException("La tercera reprogramación requiere autorización de un Administrador.");
            }

            AutorizacionTerceraPorUsuarioId = autorizacionAdministradorUsuarioId;
        }

        FechaHoraInicio = inicio;
        FechaHoraFin = fin;
        NumeroReprogramaciones = siguiente;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RegistrarLlegada()
    {
        if (Estado is not (CitaEstados.Programada or CitaEstados.Confirmada))
        {
            throw new DomainException("Solo una cita Programada o Confirmada puede pasar a espera.");
        }

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
