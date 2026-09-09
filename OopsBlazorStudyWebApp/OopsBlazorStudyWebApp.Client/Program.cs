using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
//Newly added Also verify your Client project's Program.cs has HttpClient registered.
builder.Services.AddScoped(
    sp => new HttpClient
    {
        BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
    });

await builder.Build().RunAsync();
