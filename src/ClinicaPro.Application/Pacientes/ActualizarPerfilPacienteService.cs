using ClinicaPro.Application;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public sealed class ActualizarPerfilPacienteService(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public async Task<Domain.Entities.Paciente> ExecuteAsync(
        Guid usuarioId,
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        CancellationToken cancellationToken = default)
    {
        var paciente = await pacientes.ObtenerRastreadoPorUsuarioIdAsync(usuarioId, cancellationToken)
            ?? throw new DomainException("El usuario no tiene un perfil de paciente.");

        if (!string.IsNullOrWhiteSpace(documento)
            && await pacientes.ExisteDocumentoAsync(documento, paciente.Id, cancellationToken))
        {
            throw new DomainException("Ya existe un paciente con ese documento.");
        }

        paciente.Actualizar(nombres, apellidos, documento, fechaNacimiento, telefono, direccion);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(
            usuarioId,
            "ActualizarPerfil",
            "Paciente",
            paciente.Id.ToString(),
            null,
            cancellationToken);
        return paciente;
    }
}
