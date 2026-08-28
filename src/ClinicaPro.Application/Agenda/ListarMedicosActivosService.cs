using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Agenda;

public sealed class ListarMedicosActivosService(IMedicoRepository medicos)
{
    public async Task<IReadOnlyList<(Medico Medico, IReadOnlyList<MedicoEspecialidad> Especialidades)>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var lista = await medicos.ListarActivosAsync(cancellationToken);
        var relaciones = await medicos.ListarEspecialidadesActivasAsync(cancellationToken);
        var porMedico = relaciones
            .GroupBy(relacion => relacion.MedicoId)
            .ToDictionary(grupo => grupo.Key, grupo => (IReadOnlyList<MedicoEspecialidad>)grupo.ToList());

        return lista
            .Select(medico => (
                medico,
                porMedico.TryGetValue(medico.Id, out var especialidades)
                    ? especialidades
                    : Array.Empty<MedicoEspecialidad>()))
            .ToList();
    }
}
