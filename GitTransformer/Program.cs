using GitTransformer;
using BlazorMonaco.Editor;
using GitTransformer.Application.Abstractions;
using GitTransformer.Application.Services;
using GitTransformer.Browser;
using GitTransformer.Core.Abstractions;
using GitTransformer.Infrastructure.Http;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Radzen;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services
    .AddScoped<AppData>()
    .AddScoped<IJsTransformStore, IndexedDbJsTransformStore>()
    .AddScoped<ITextTransformationService, TextTransformationService>()
    .AddScoped<IQuoteService, QuoteService>()
    .AddScoped<IRemoteQuoteSource>(_ => new RemoteQuoteSource(new HttpClient
        { BaseAddress = new Uri("https://qapi.vercel.app/api/") }))
    .AddScoped(_ => new LocalContentRepository(new HttpClient
        { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) }))
    .AddScoped<ILocalContentRepository>(services => services.GetRequiredService<LocalContentRepository>())
    .AddScoped<IEditorThemeProvider<StandaloneThemeData>>(services => services.GetRequiredService<LocalContentRepository>())
    .AddRadzenComponents();

await builder.Build().RunAsync();
