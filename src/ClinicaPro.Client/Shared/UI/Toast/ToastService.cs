namespace ClinicaPro.Client.Shared.UI.Toast;

public enum ToastTipo
{
    Exito,
    Error,
    Info
}

public sealed record ToastMensaje(Guid Id, ToastTipo Tipo, string Texto);

public sealed class ToastService
{
    private readonly List<ToastMensaje> _mensajes = [];

    public event Action? Cambio;

    public IReadOnlyList<ToastMensaje> Mensajes => _mensajes;

    public void Exito(string texto) => Agregar(ToastTipo.Exito, texto);

    public void Error(string texto) => Agregar(ToastTipo.Error, texto);

    public void Info(string texto) => Agregar(ToastTipo.Info, texto);

    public void Cerrar(Guid id)
    {
        _mensajes.RemoveAll(m => m.Id == id);
        Cambio?.Invoke();
    }

    private void Agregar(ToastTipo tipo, string texto)
    {
        var mensaje = new ToastMensaje(Guid.NewGuid(), tipo, texto);
        _mensajes.Add(mensaje);
        Cambio?.Invoke();

        _ = QuitarLuegoAsync(mensaje.Id);
    }

    private async Task QuitarLuegoAsync(Guid id)
    {
        await Task.Delay(4500);
        Cerrar(id);
    }
}
