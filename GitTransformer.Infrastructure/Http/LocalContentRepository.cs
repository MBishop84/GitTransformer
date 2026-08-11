using BlazorMonaco.Editor;
using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;
using System.Net.Http.Json;

namespace GitTransformer.Infrastructure.Http;

public sealed class LocalContentRepository :
    ILocalContentRepository,
    IEditorThemeProvider<StandaloneThemeData>
{
    private readonly HttpClient _httpClient;
    private readonly Lazy<Task<IReadOnlyList<Quote>>> _quotes;
    private readonly Lazy<Task<IReadOnlyList<string>>> _themes;
    private readonly Lazy<Task<IReadOnlyList<JsTransform>>> _jsTransforms;

    public LocalContentRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _quotes = new(LoadQuotesAsync);
        _themes = new(LoadThemesAsync);
        _jsTransforms = new(LoadJsTransformsAsync);
    }

    public async Task<Quote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default)
    {
        var quotes = await _quotes.Value.WaitAsync(cancellationToken);
        return quotes.Count == 0 ? null : quotes[Random.Shared.Next(quotes.Count)];
    }

    public Task<IReadOnlyList<string>> GetMonacoThemesAsync(CancellationToken cancellationToken = default)
        => _themes.Value.WaitAsync(cancellationToken);

    public Task<IReadOnlyList<JsTransform>> GetFileTransformsAsync(CancellationToken cancellationToken = default)
        => _jsTransforms.Value.WaitAsync(cancellationToken);

    public Task<StandaloneThemeData?> GetThemeAsync(
        string theme,
        CancellationToken cancellationToken = default)
        => _httpClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json", cancellationToken);

    private async Task<IReadOnlyList<Quote>> LoadQuotesAsync()
    {
        var quotes = new List<Quote>();
        await foreach (var quote in _httpClient.GetFromJsonAsAsyncEnumerable<Quote>("data/quotes.json"))
        {
            if (quote is not null)
            {
                quotes.Add(quote);
            }
        }

        return quotes;
    }

    private async Task<IReadOnlyList<string>> LoadThemesAsync()
    {
        var themes = await _httpClient.GetFromJsonAsync<Dictionary<string, string>>("themes/themelist.json");
        return themes is null ? [] : [.. themes.Values];
    }

    private async Task<IReadOnlyList<JsTransform>> LoadJsTransformsAsync()
    {
        var transforms = new List<JsTransform>();
        await foreach (var transform in _httpClient.GetFromJsonAsAsyncEnumerable<JsTransform>("data/JsTransforms.json"))
        {
            if (transform is not null)
            {
                transforms.Add(transform);
            }
        }

        return transforms;
    }
}
