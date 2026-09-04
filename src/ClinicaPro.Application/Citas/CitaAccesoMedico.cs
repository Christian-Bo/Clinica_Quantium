using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Citas;

public static class CitaAccesoMedico
{
    public static void ExigirAsignado(Cita cita, Guid medicoId)
    {
        if (cita.MedicoId != medicoId)
        {
            throw new ForbiddenException("Solo el médico asignado puede iniciar o finalizar esta cita.");
        }
    }
}
