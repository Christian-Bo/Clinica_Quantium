namespace ClinicaPro.Client.Features.Paciente.Components;

/// <summary>
/// Permite que el mismo formulario se pinte completo (Mi perfil) o por partes
/// (el registro, que va por pasos). Sin esto habría que duplicar el marcado y
/// mantener dos copias de los mismos campos.
/// </summary>
public enum SeccionFormulario
{
    Completo,
    Cuenta,
    DatosPersonales,
    Salud
}
