using System.Net.Http.Headers;

namespace ClinicaPro.Client.Shared.Auth;

public sealed class BearerTokenHandler(TokenStorageService tokenStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var sesion = await tokenStorage.ObtenerAsync();
        if (sesion is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
