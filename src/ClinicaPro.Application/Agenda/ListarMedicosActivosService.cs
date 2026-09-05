using ClinicaPro.Domain.Entities;

namespace ClinicaPro.Application.Agenda;

public sealed class ListarMedicosActivosService(IMedicoRepository medicos)
{
    public Task<IReadOnlyList<Medico>> ExecuteAsync(CancellationToken cancellationToken = default)
        => medicos.ListarActivosAsync(cancellationToken);
}