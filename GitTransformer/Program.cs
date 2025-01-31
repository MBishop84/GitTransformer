using GitTransformer;
using GitTransformer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddSingleton<QuotableApiService>()
    .AddSingleton<LocalFileService>()
    .AddKeyedSingleton("quotable", (_, _) => new HttpClient()
        { BaseAddress = new Uri("https://qapi.vercel.app/api/") })
    .AddKeyedSingleton("local", (_, _) => new HttpClient
        { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddRadzenComponents();

await builder.Build().RunAsync();
