using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace ClinicaPro.Client;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        var apiBaseUrl = builder.Configuration["ApiBaseUrl"]
            ?? "https://localhost:7041/";

        builder.Services.AddScoped(_ => new HttpClient
        {
            BaseAddress = new Uri(apiBaseUrl)
        });

        await builder.Build().RunAsync();
    }
}
