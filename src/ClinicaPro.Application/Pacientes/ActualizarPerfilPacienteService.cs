using ClinicaPro.Application;
using ClinicaPro.Domain.Entities;
using ClinicaPro.Domain.Exceptions;

namespace ClinicaPro.Application.Pacientes;

public sealed class ActualizarPerfilPacienteService(
    IPacienteRepository pacientes,
    IUnitOfWork unitOfWork,
    IAuditoriaWriter auditoria)
{
    public Task<Paciente> ExecuteAsync(
        Guid usuarioId,
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        string? sexo,
        string? alergias,
        string? contactoEmergenciaNombre,
        string? contactoEmergenciaTelefono,
        CancellationToken cancellationToken = default)
        => EjecutarAsync(
            () => pacientes.ObtenerRastreadoPorUsuarioIdAsync(usuarioId, cancellationToken),
            usuarioId,
            "ActualizarPerfil",
            nombres,
            apellidos,
            documento,
            fechaNacimiento,
            telefono,
            direccion,
            sexo,
            alergias,
            contactoEmergenciaNombre,
            contactoEmergenciaTelefono,
            cancellationToken);

    public Task<Paciente> ExecutePorPacienteIdAsync(
        Guid actorUsuarioId,
        Guid pacienteId,
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        string? sexo,
        string? alergias,
        string? contactoEmergenciaNombre,
        string? contactoEmergenciaTelefono,
        CancellationToken cancellationToken = default)
        => EjecutarAsync(
            () => pacientes.ObtenerRastreadoPorIdAsync(pacienteId, cancellationToken),
            actorUsuarioId,
            "ActualizarPacienteStaff",
            nombres,
            apellidos,
            documento,
            fechaNacimiento,
            telefono,
            direccion,
            sexo,
            alergias,
            contactoEmergenciaNombre,
            contactoEmergenciaTelefono,
            cancellationToken);

    private async Task<Paciente> EjecutarAsync(
        Func<Task<Paciente?>> obtener,
        Guid actorUsuarioId,
        string accion,
        string nombres,
        string apellidos,
        string? documento,
        DateOnly? fechaNacimiento,
        string? telefono,
        string? direccion,
        string? sexo,
        string? alergias,
        string? contactoEmergenciaNombre,
        string? contactoEmergenciaTelefono,
        CancellationToken cancellationToken)
    {
        var paciente = await obtener()
            ?? throw new DomainException("El paciente no existe.");

        if (!string.IsNullOrWhiteSpace(documento)
            && await pacientes.ExisteDocumentoAsync(documento, paciente.Id, cancellationToken))
        {
            throw new DomainException("Ya existe un paciente con ese documento.");
        }

        paciente.Actualizar(
            nombres,
            apellidos,
            documento,
            fechaNacimiento,
            telefono,
            direccion,
            sexo,
            alergias,
            contactoEmergenciaNombre,
            contactoEmergenciaTelefono);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await auditoria.RegistrarAsync(
            actorUsuarioId,
            accion,
            "Paciente",
            paciente.Id.ToString(),
            null,
            cancellationToken);
        return paciente;
    }
}
