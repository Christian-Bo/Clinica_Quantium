namespace ClinicaPro.Contracts.Pacientes;

public sealed record PacienteDto(
    Guid PacienteId,
    Guid UsuarioId,
    string Nombres,
    string Apellidos,
    string NombreCompleto,
    string? Documento,
    DateOnly? FechaNacimiento,
    string? Telefono,
    string? Direccion,
    string? Sexo,
    string? Alergias,
    string? ContactoEmergenciaNombre,
    string? ContactoEmergenciaTelefono);
