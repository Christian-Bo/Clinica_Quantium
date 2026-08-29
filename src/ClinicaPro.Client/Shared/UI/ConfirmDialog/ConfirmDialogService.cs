namespace ClinicaPro.Client.Shared.UI.ConfirmDialog;

public sealed record ConfirmDialogSolicitud(
    string Titulo,
    string Mensaje,
    string TextoConfirmar,
    bool EsPeligroso);

public sealed class ConfirmDialogService
{
    private TaskCompletionSource<bool>? _pendiente;

    public event Action? Cambio;

    public ConfirmDialogSolicitud? SolicitudActual { get; private set; }

    public Task<bool> ConfirmarAsync(
        string mensaje,
        string titulo = "Confirmar acción",
        string textoConfirmar = "Confirmar",
        bool esPeligroso = false)
    {
        _pendiente?.TrySetResult(false);

        SolicitudActual = new ConfirmDialogSolicitud(titulo, mensaje, textoConfirmar, esPeligroso);
        _pendiente = new TaskCompletionSource<bool>();
        Cambio?.Invoke();

        return _pendiente.Task;
    }

    public void Resolver(bool resultado)
    {
        SolicitudActual = null;
        _pendiente?.TrySetResult(resultado);
        _pendiente = null;
        Cambio?.Invoke();
    }
}
