using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;
using System.Net.Http.Json;

namespace GitTransformer.Infrastructure.Http;

public sealed class RemoteQuoteSource(HttpClient httpClient) : IRemoteQuoteSource
{
    public async Task<Quote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<QuoteResponse>("random", cancellationToken);
        return response is null
            ? null
            : new Quote(0, response.Quote, new Author(0, response.Author));
    }

    private sealed record QuoteResponse(string Quote, string Author);
}
