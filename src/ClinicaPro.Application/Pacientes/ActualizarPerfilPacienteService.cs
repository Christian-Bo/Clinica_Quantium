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
            permitirCambiarDocumento: false,
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
            permitirCambiarDocumento: true,
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
        bool permitirCambiarDocumento,
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

        var documentoFinal = ResolverDocumento(paciente.Documento, documento, permitirCambiarDocumento);

        if (!string.IsNullOrWhiteSpace(documentoFinal)
            && await pacientes.ExisteDocumentoAsync(documentoFinal, paciente.Id, cancellationToken))
        {
            throw new DomainException("Ya existe un paciente con ese documento.");
        }

        paciente.Actualizar(
            nombres,
            apellidos,
            documentoFinal,
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

    private static string? ResolverDocumento(string? actual, string? propuesto, bool permitirCambiarDocumento)
    {
        if (permitirCambiarDocumento)
        {
            return propuesto;
        }

        if (string.IsNullOrWhiteSpace(actual))
        {
            return propuesto;
        }

        if (!string.IsNullOrWhiteSpace(propuesto)
            && !string.Equals(propuesto.Trim(), actual.Trim(), StringComparison.Ordinal))
        {
            throw new DomainException(
                "El documento de identidad no se puede cambiar desde el portal. Solicite la corrección en recepción.");
        }

        return actual;
    }
}
