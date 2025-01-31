using GitTransformer;
using GitTransformer.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddScoped<QuotableApiService>()
    .AddScoped<LocalFileService>()
    .AddKeyedScoped("quotable", (_, _) => {
        return new HttpClient()
        {
            BaseAddress = new Uri("https://qapi.vercel.app/api/")
        };})
    .AddKeyedScoped("local", (_, _) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
    .AddRadzenComponents();

await builder.Build().RunAsync();
