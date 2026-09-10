namespace ClinicaPro.Client.Features.Paciente.Components;

/// <summary>
/// La cita que el paciente agenda durante el registro público.
///
/// La API exige médico, fecha y motivo en el mismo POST que crea la cuenta:
/// o entran las dos cosas o no entra ninguna. Por eso la selección vive en un
/// modelo aparte que el formulario valida antes de enviar, en vez de dejar que
/// el servidor rechace la petición completa.
/// </summary>
public sealed class ModeloPrimeraCita
{
    public const int MotivoMaxLength = 500;

    /// <summary>Hueco elegido. Trae médico, inicio y fin en una sola pieza.</summary>
    public SlotDisponibleDto? Slot { get; set; }

    public string Motivo { get; set; } = string.Empty;

    /// <summary>
    /// Fecha que se está consultando, en formato ISO porque es lo que espera
    /// un input de tipo date. Arranca en mañana: la agenda no acepta citas
    /// para hoy.
    /// </summary>
    public string FechaTexto { get; set; } = PrimeraFechaPosible.ToString("yyyy-MM-dd");

    public static DateOnly PrimeraFechaPosible => DateOnly.FromDateTime(DateTime.Today.AddDays(1));

    public Dictionary<string, string> Validar()
    {
        var errores = new Dictionary<string, string>();

        if (Slot is null)
        {
            errores[nameof(Slot)] = "Elige un médico y uno de sus horarios disponibles.";
        }

        var motivo = Motivo.Trim();
        if (motivo.Length == 0)
        {
            errores[nameof(Motivo)] = "Cuéntanos brevemente qué te trae a consulta.";
        }
        else if (motivo.Length > MotivoMaxLength)
        {
            errores[nameof(Motivo)] = $"El motivo no puede pasar de {MotivoMaxLength} caracteres.";
        }

        return errores;
    }
}
