using BlazorMonaco.Editor;
using System.Net.Http.Json;

namespace GitTransformer.Services;

public class LocalFileService
{
    private readonly HttpClient _httpClient;
    private readonly List<Quote?> _quotes = [];
    private readonly Dictionary<string, string> _themes = [];
    private readonly List<JsTransform?> _jsTransforms = [];
    private readonly Dictionary<string, Task> StartupTasks;

    public LocalFileService([FromKeyedServices("local")] HttpClient httpClient)
    {
        _httpClient = httpClient;
        StartupTasks = new Dictionary<string, Task>
        {
            ["GetThemes"] = Task.Run(async () =>
            {
                var themes = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>("themes/themelist.json");
                if(themes is not null)
                    foreach (var theme in themes.AsParallel())
                        _themes.TryAdd(theme.Key, theme.Value);
            }),
            ["PopulateQuotes"] = Task.Run(async () =>
            {
                await foreach (var quote in _httpClient.GetFromJsonAsAsyncEnumerable<Quote>("data/quotes.json"))
                    _quotes.Add(quote);
            }),
            ["GetJsTransforms"] = Task.Run(async () =>
            {
                await foreach (var transform in _httpClient.GetFromJsonAsAsyncEnumerable<JsTransform>("data/JsTransforms.json"))
                    _jsTransforms.Add(transform);
            })
        };
    }

    public async Task<Quote?> GetRandomQuote()
    {
        var thisTask = StartupTasks["PopulateQuotes"];
        if (thisTask.IsCompleted)
            return _quotes![new Random().Next(_quotes.Count)]!;
        else
        {
            await thisTask;
            return _quotes![new Random().Next(_quotes.Count)]!;
        }
    }

    public async Task<Dictionary<string, string>> GetMonacoThemes()
    {
        var thisTask = StartupTasks["GetThemes"];
        if (thisTask.IsCompleted)
            return _themes;
        else
        {
            await thisTask;
            return _themes;
        }
    }

    public Task<StandaloneThemeData?> GetStandaloneThemeData(string theme)
        => _httpClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json");

    public async Task<List<JsTransform?>> GetFileTransforms()
    {
        var thisTask = StartupTasks["GetJsTransforms"];

        if (thisTask.IsCompleted)
            return _jsTransforms;
        else
        {
            await thisTask;
            return _jsTransforms;
        }
    }
}