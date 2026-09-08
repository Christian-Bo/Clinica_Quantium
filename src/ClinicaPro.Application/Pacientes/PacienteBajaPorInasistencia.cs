using ClinicaPro.Domain;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public static class PacienteBajaPorInasistencia
{
    public static void Exigir(IReadOnlyList<Cita> citas)
    {
        if (citas.Count == 0)
        {
            throw new DomainException(
                "El paciente no tiene citas. Solo se da de baja si faltó a su primera cita.");
        }

        var primera = citas
            .OrderBy(cita => cita.CreatedAtUtc)
            .ThenBy(cita => cita.FechaHoraInicio)
            .First();

        if (primera.Estado != CitaEstados.NoPresentada)
        {
            throw new DomainException(
                "Primero secretaría debe marcar la primera cita como No presentada. Después el administrador puede eliminar al paciente.");
        }

        if (citas.Any(cita => cita.Estado is CitaEstados.Atendida or CitaEstados.EnAtencion or CitaEstados.EnEspera))
        {
            throw new DomainException(
                "No se puede eliminar: el paciente ya tuvo atención clínica.");
        }
    }
}
