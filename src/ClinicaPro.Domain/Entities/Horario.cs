using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Domain.Entities;

public sealed class Horario
{
    public Guid Id { get; private set; }
    public Guid MedicoId { get; private set; }
    public byte DiaSemana { get; private set; }
    public TimeOnly HoraInicio { get; private set; }
    public TimeOnly HoraFin { get; private set; }
    public DateOnly? VigenteDesde { get; private set; }
    public DateOnly? VigenteHasta { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }

    private Horario()
    {
    }

    public static Horario Create(
        Guid medicoId,
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly? vigenteDesde = null,
        DateOnly? vigenteHasta = null)
    {
        Validar(diaSemana, horaInicio, horaFin, vigenteDesde, vigenteHasta);

        return new Horario
        {
            Id = Guid.NewGuid(),
            MedicoId = medicoId,
            DiaSemana = diaSemana,
            HoraInicio = horaInicio,
            HoraFin = horaFin,
            VigenteDesde = vigenteDesde,
            VigenteHasta = vigenteHasta,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
    }

    public void Actualizar(
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly? vigenteDesde,
        DateOnly? vigenteHasta,
        bool isActive)
    {
        Validar(diaSemana, horaInicio, horaFin, vigenteDesde, vigenteHasta);
        DiaSemana = diaSemana;
        HoraInicio = horaInicio;
        HoraFin = horaFin;
        VigenteDesde = vigenteDesde;
        VigenteHasta = vigenteHasta;
        IsActive = isActive;
    }

    public void Desactivar() => IsActive = false;

    private static void Validar(
        byte diaSemana,
        TimeOnly horaInicio,
        TimeOnly horaFin,
        DateOnly? vigenteDesde,
        DateOnly? vigenteHasta)
    {
        if (diaSemana is < 1 or > 7)
        {
            throw new DomainException("El día de la semana debe estar entre 1 (lunes) y 7 (domingo).");
        }

        if (horaFin <= horaInicio)
        {
            throw new DomainException("La hora de fin debe ser posterior a la de inicio.");
        }

        if (vigenteDesde is not null && vigenteHasta is not null && vigenteHasta < vigenteDesde)
        {
            throw new DomainException("VigenteHasta no puede ser anterior a VigenteDesde.");
        }
    }
}
