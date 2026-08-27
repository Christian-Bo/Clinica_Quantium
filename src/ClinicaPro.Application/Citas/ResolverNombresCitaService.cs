using ClinicaPro.Application.Agenda;
using ClinicaPro.Application.Especialidades;
using ClinicaPro.Application.Pacientes;
using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Citas;

public sealed class ResolverNombresCitaService(
    IPacienteRepository pacientes,
    IMedicoRepository medicos,
    IEspecialidadRepository especialidades)
{
    public async Task<IReadOnlyDictionary<Guid, NombresCita>> ExecuteAsync(
        IReadOnlyList<Cita> citas,
        CancellationToken cancellationToken = default)
    {
        if (citas.Count == 0)
        {
            return new Dictionary<Guid, NombresCita>();
        }

        var pacienteIds = citas.Select(cita => cita.PacienteId).Distinct().ToList();
        var listaPacientes = await pacientes.ListarPorIdsAsync(pacienteIds, cancellationToken);
        var porPaciente = listaPacientes.ToDictionary(item => item.Id, item => item.NombreCompleto);

        var porMedico = (await medicos.ListarTodosAsync(cancellationToken))
            .ToDictionary(item => item.Id, item => item.NombreCompleto);

        var porEspecialidad = (await especialidades.ListarTodasAsync(cancellationToken))
            .ToDictionary(item => item.Id, item => item.Nombre);

        return citas.ToDictionary(
            cita => cita.Id,
            cita => new NombresCita(
                porPaciente.GetValueOrDefault(cita.PacienteId, string.Empty),
                porMedico.GetValueOrDefault(cita.MedicoId, string.Empty),
                porEspecialidad.GetValueOrDefault(cita.EspecialidadId, string.Empty)));
    }
}

public sealed record NombresCita(string PacienteNombre, string MedicoNombre, string EspecialidadNombre);
