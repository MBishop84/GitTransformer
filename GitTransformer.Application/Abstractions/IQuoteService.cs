using GitTransformer.Core.Models;

namespace GitTransformer.Application.Abstractions;

public interface IQuoteService
{
    Task<Quote> GetRandomQuoteAsync(CancellationToken cancellationToken = default);
}
