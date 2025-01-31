using BlazorMonaco.Editor;
using Radzen;
using System.Net.Http.Json;

namespace GitTransformer.Services;

public class LocalFileService
{
    private readonly HttpClient _httpClient;
    private readonly List<Quote?> _quotes = [];
    private readonly Dictionary<string, string> _themes = [];
    private readonly List<JsTransform?> _jsTransforms = [];
    private readonly Task<Quote?> PopulateQuotes;
    private readonly Task<List<JsTransform?>> GetJsTransforms;
    private readonly Task<Dictionary<string, string>> GetThemes;

    public LocalFileService([FromKeyedServices("local")] HttpClient httpClient)
    {
        _httpClient = httpClient;
        GetThemes = Task.Run(async () =>
        {
            var themes = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>("themes/themelist.json");
            if(themes is not null)
                foreach (var theme in themes)
                    _themes.TryAdd(theme.Key, theme.Value);
            return _themes;
        });
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

    public async Task<Dictionary<string, string>> GetMonacoThemes()
    {
        if (GetThemes.IsCompleted)
            return _themes;
        else
        {
            await GetThemes;
            return _themes;
        }
    }

    public Task<StandaloneThemeData?> GetStandaloneThemeData(string theme)
        => _httpClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json");

    public async Task<List<JsTransform?>> GetFileTransforms()
    {
        if (GetJsTransforms.IsCompleted)
            return _jsTransforms;
        else
        {
            await GetJsTransforms;
            return _jsTransforms;
        }
    }
}