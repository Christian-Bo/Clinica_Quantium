using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ClinicaPro.Client.Shared.Auth;

public sealed class ApiAuthenticationStateProvider(TokenStorageService tokenStorage) : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonimo = new(new ClaimsPrincipal(new ClaimsIdentity()));

    private AuthenticationState? _estadoActual;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_estadoActual is not null)
        {
            return _estadoActual;
        }

        var sesion = await tokenStorage.ObtenerAsync();
        _estadoActual = sesion is null ? Anonimo : Construir(sesion);
        return _estadoActual;
    }

    public void NotificarSesionIniciada(AuthResponse sesion)
    {
        _estadoActual = Construir(sesion);
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }

    /// <summary>
    /// Refresca los claims de la UI con /api/auth/me sin sustituir el JWT.
    /// Así cambios de rol o MustChangePassword se reflejan al arrancar la app.
    /// </summary>
    public void NotificarUsuarioValidado(UsuarioActualDto usuario)
    {
        _estadoActual = Construir(usuario);
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }

    public void NotificarSesionCerrada()
    {
        _estadoActual = Anonimo;
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }

    private static AuthenticationState Construir(AuthResponse sesion)
        => Construir(sesion.UsuarioId, sesion.Email, sesion.Roles);

    private static AuthenticationState Construir(UsuarioActualDto usuario)
        => Construir(usuario.UsuarioId, usuario.Email, usuario.Roles);

    private static AuthenticationState Construir(
        Guid usuarioId,
        string email,
        IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuarioId.ToString()),
            new(ClaimTypes.Name, email)
        };

        claims.AddRange(roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var identidad = new ClaimsIdentity(claims, authenticationType: "ClinicaProJwt");
        return new AuthenticationState(new ClaimsPrincipal(identidad));
    }
}
