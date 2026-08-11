using GitTransformer.Application.Abstractions;
using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;

namespace GitTransformer.Application.Services;

public sealed class QuoteService(
    IRemoteQuoteSource remoteQuoteSource,
    ILocalContentRepository localContentRepository) : IQuoteService
{
    public async Task<Quote> GetRandomQuoteAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var remoteQuote = await remoteQuoteSource.GetRandomQuoteAsync(cancellationToken);
            if (remoteQuote is not null && remoteQuote.Author.Name != "Unknown")
            {
                return remoteQuote;
            }
        }
        catch (HttpRequestException)
        {
        }

        return await localContentRepository.GetRandomQuoteAsync(cancellationToken) ?? Quote.Empty;
    }
}
