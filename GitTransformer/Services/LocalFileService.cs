using BlazorMonaco.Editor;
using System.Net.Http.Json;

namespace GitTransformer.Services;

public class LocalFileService
{
    private readonly HttpClient _httpClient;
    private readonly List<Quote?> _quotes = [];
    private readonly Dictionary<string, string> _themes = [];
    private readonly List<JsTransform?> _jsTransforms = [];
    private readonly Task<Quote?> PopulateQuotes;
    public Task<List<JsTransform?>> GetJsTransforms { get; }

    public LocalFileService([FromKeyedServices("local")] HttpClient httpClient)
    {
        _httpClient = httpClient;
        PopulateQuotes = Task.Run(async () =>
        {
            await foreach (var quote in _httpClient.GetFromJsonAsAsyncEnumerable<Quote>("data/quotes.json"))
                _quotes.Add(quote);
            return _quotes[new Random().Next(_quotes.Count)];
        });
        GetJsTransforms = Task.Run(async () =>
        {
            await foreach (var transform in _httpClient.GetFromJsonAsAsyncEnumerable<JsTransform>("data/js-transforms.json"))
                _jsTransforms.Add(transform);
            return _jsTransforms;
        });
    }

    public async Task<Quote?> GetRandomQuote()
    {
        if ((_quotes?.Count ?? 0) > 0)
            return _quotes![new Random().Next(_quotes.Count)]!;
        else
            return await PopulateQuotes;
    }

    public async Task<Dictionary<string, string>> GetThemes()
    {
        if (_themes.Count > 0)
            return _themes;
        else
        {
            await foreach (var theme in _httpClient.GetFromJsonAsAsyncEnumerable<KeyValuePair<string, string>>("data/themes.json"))
                _themes.TryAdd(theme.Key, theme.Value);
            return _themes;
        }
    }

    public Task<StandaloneThemeData?> GetStandaloneThemeData(string theme)
        => _httpClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json");

    public async Task<IEnumerable<JsTransform?>> GetFileTransforms()
    {
        if ((_jsTransforms?.Count ?? 0) > 0)
            return _jsTransforms!;
        else
            return await GetJsTransforms;
    }
}