using GitTransformer.Core.Models;

namespace GitTransformer.Core.Abstractions;

public interface IQuoteSource
{
    Task<Quote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default);
}
