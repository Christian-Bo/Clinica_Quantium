using ClinicaPro.Client.Features.Secretaria.Services;
using ClinicaPro.Client.Shared.Auth;
using ClinicaPro.Client.Shared.UI.ConfirmDialog;
using ClinicaPro.Client.Shared.UI.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ClinicaPro.Client;

public static class Program
{
    private const string NombreClienteApi = "ClinicaPro.Api";

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "https://localhost:7041/";

        // --- Autenticación ---
        builder.Services.AddAuthorizationCore();
        builder.Services.AddScoped<TokenStorageService>();
        builder.Services.AddScoped<ApiAuthenticationStateProvider>();
        builder.Services.AddScoped<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
        builder.Services.AddScoped<AuthApiService>();

        // --- HttpClient con el token Bearer inyectado automáticamente ---
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services
            .AddHttpClient(NombreClienteApi, cliente => cliente.BaseAddress = new Uri(apiBaseUrl))
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(NombreClienteApi));

        // --- UI transversal (toasts, confirmaciones) ---
        builder.Services.AddScoped<ToastService>();
        builder.Services.AddScoped<ConfirmDialogService>();

        // --- Servicios de API del módulo de Secretaría ---
        builder.Services.AddScoped<EspecialidadesApiService>();
        builder.Services.AddScoped<MedicosApiService>();
        builder.Services.AddScoped<PacientesApiService>();
        builder.Services.AddScoped<CitasApiService>();
        builder.Services.AddScoped<NotificacionesApiService>();
        builder.Services.AddScoped<ReportesApiService>();

        await builder.Build().RunAsync();
    }
}
