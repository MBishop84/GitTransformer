using BlazorMonaco.Editor;
using System.Net.Http.Json;

namespace GitTransformer.Services;

public class LocalFileService
{
    private readonly HttpClient _httpClient;
    private readonly Lazy<Task<List<Quote?>>> _quotes;
    private readonly Lazy<Task<List<string>>> _themes;
    private readonly Lazy<Task<List<JsTransform?>>> _jsTransforms;

    public LocalFileService([FromKeyedServices("local")] HttpClient httpClient)
    {
        _httpClient = httpClient;
        _quotes = new(LoadQuotes);
        _themes = new(LoadThemes);
        _jsTransforms = new(LoadJsTransforms);
    }

    public async Task<Quote?> GetRandomQuote()
    {
        var quotes = await _quotes.Value;
        return quotes.Count == 0 ? null : quotes[Random.Shared.Next(quotes.Count)];
    }

    public Task<List<string>> GetMonacoThemes() => _themes.Value;

    public Task<StandaloneThemeData?> GetStandaloneThemeData(string theme)
        => _httpClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json");

    public Task<List<JsTransform?>> GetFileTransforms() => _jsTransforms.Value;

    private async Task<List<Quote?>> LoadQuotes()
    {
        List<Quote?> quotes = [];
        await foreach (var quote in _httpClient.GetFromJsonAsAsyncEnumerable<Quote>("data/quotes.json"))
            quotes.Add(quote);
        return quotes;
    }

    private async Task<List<string>> LoadThemes()
    {
        var themes = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>("themes/themelist.json");
        return themes is null ? [] : [.. themes.Values];
    }

    private async Task<List<JsTransform?>> LoadJsTransforms()
    {
        List<JsTransform?> transforms = [];
        await foreach (var transform in _httpClient.GetFromJsonAsAsyncEnumerable<JsTransform>("data/JsTransforms.json"))
            transforms.Add(transform);
        return transforms;
    }
}