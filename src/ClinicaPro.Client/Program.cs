using ClinicaPro.Client.Features.Paciente.Services;
using ClinicaPro.Client.Features.Secretaria.Services;
using ClinicaPro.Client.Features.Admin.Services;
using ClinicaPro.Client.Features.Medico.Services;
using ClinicaPro.Client.Shared.Auth;
using ClinicaPro.Client.Shared.UI.ConfirmDialog;
using ClinicaPro.Client.Shared.UI.Toast;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicaPro.Client;

public static class Program
{
    private const string NombreClienteApi = "ClinicaPro.Api";

    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Mantener la consola del navegador enfocada en problemas reales.
        // Los mensajes informativos de autorización/HttpClient son esperables durante
        // el arranque y no aportan valor al usuario ni al diagnóstico cotidiano.
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.Authorization", LogLevel.Warning);
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://136.113.48.173:5030/";

        // --- Autenticación ---
        builder.Services.AddAuthorizationCore();
        // TokenStorage y el AuthenticationStateProvider deben ser instancias únicas
        // de la SPA. IHttpClientFactory crea sus propios scopes para handlers; si
        // estos servicios fueran scoped, un handler creado antes del login podría
        // quedarse con una caché nula y enviar las primeras llamadas sin Bearer.
        builder.Services.AddSingleton<TokenStorageService>();
        builder.Services.AddSingleton<ApiAuthenticationStateProvider>();
        builder.Services.AddSingleton<AuthenticationStateProvider>(
            sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
        builder.Services.AddScoped<AuthApiService>();
        builder.Services.AddScoped<UiPermissionService>();

        // --- HttpClient con el token Bearer inyectado automáticamente ---
        builder.Services.AddTransient<BearerTokenHandler>();
        builder.Services
            .AddHttpClient(NombreClienteApi, cliente =>
            {
                cliente.BaseAddress = new Uri(apiBaseUrl);
                cliente.Timeout = TimeSpan.FromSeconds(30);
            })
            .AddHttpMessageHandler<BearerTokenHandler>();
        builder.Services.AddScoped(sp =>
            sp.GetRequiredService<IHttpClientFactory>().CreateClient(NombreClienteApi));

        builder.Services.AddScoped<ApiClient>();

        // --- UI transversal (toasts, confirmaciones) ---
        builder.Services.AddScoped<ToastService>();
        builder.Services.AddScoped<ConfirmDialogService>();

        // --- Servicios de API del módulo de Administración ---
        builder.Services.AddScoped<AdminApiService>();
        builder.Services.AddScoped<AgendaMedicoRealtimeService>();

        // --- Servicios de API del módulo de Secretaría ---
        builder.Services.AddScoped<MedicosCacheService>();
        builder.Services.AddScoped<MedicosApiService>();
        builder.Services.AddScoped<PacientesApiService>();
        builder.Services.AddScoped<CitasApiService>();
        builder.Services.AddScoped<NotificacionesApiService>();

        // --- Servicios de API del portal del Paciente ---
        builder.Services.AddScoped<PerfilPacienteApiService>();
        builder.Services.AddScoped<CitasPacienteApiService>();
        builder.Services.AddScoped<NotificacionesPacienteApiService>();

        await builder.Build().RunAsync();
    }
}
