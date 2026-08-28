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

    public void NotificarSesionCerrada()
    {
        _estadoActual = Anonimo;
        NotifyAuthenticationStateChanged(Task.FromResult(_estadoActual));
    }

    private static AuthenticationState Construir(AuthResponse sesion)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sesion.UsuarioId.ToString()),
            new(ClaimTypes.Name, sesion.Email)
        };

        claims.AddRange(sesion.Roles.Select(rol => new Claim(ClaimTypes.Role, rol)));

        var identidad = new ClaimsIdentity(claims, authenticationType: "ClinicaProJwt");
        return new AuthenticationState(new ClaimsPrincipal(identidad));
    }
}
