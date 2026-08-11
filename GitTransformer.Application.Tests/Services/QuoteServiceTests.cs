using GitTransformer.Application.Services;
using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;

namespace GitTransformer.Application.Tests.Services;

public sealed class QuoteServiceTests
{
    [Fact]
    public async Task GetRandomQuoteAsync_ReturnsRemoteQuoteWhenAvailable()
    {
        var remoteQuote = new Quote(0, "Remote", new Author(0, "Author"));
        var localSource = new StubLocalContentRepository(new Quote(1, "Local", new Author(1, "Local Author")));
        var service = new QuoteService(new StubRemoteQuoteSource(remoteQuote), localSource);

        var result = await service.GetRandomQuoteAsync();

        Assert.Equal(remoteQuote, result);
        Assert.Equal(0, localSource.RequestCount);
    }

    [Fact]
    public async Task GetRandomQuoteAsync_FallsBackWhenRemoteQuoteIsUnknown()
    {
        var localQuote = new Quote(1, "Local", new Author(1, "Local Author"));
        var service = new QuoteService(
            new StubRemoteQuoteSource(new Quote(0, string.Empty, new Author())),
            new StubLocalContentRepository(localQuote));

        var result = await service.GetRandomQuoteAsync();

        Assert.Equal(localQuote, result);
    }

    [Fact]
    public async Task GetRandomQuoteAsync_FallsBackWhenRemoteRequestFails()
    {
        var localQuote = new Quote(1, "Local", new Author(1, "Local Author"));
        var service = new QuoteService(
            new StubRemoteQuoteSource(exception: new HttpRequestException("Unavailable")),
            new StubLocalContentRepository(localQuote));

        var result = await service.GetRandomQuoteAsync();

        Assert.Equal(localQuote, result);
    }

    private sealed class StubRemoteQuoteSource(
        Quote? quote = null,
        Exception? exception = null) : IRemoteQuoteSource
    {
        public Task<Quote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default)
            => exception is null
                ? Task.FromResult(quote)
                : Task.FromException<Quote?>(exception);
    }

    private sealed class StubLocalContentRepository(Quote? quote) : ILocalContentRepository
    {
        public int RequestCount { get; private set; }

        public Task<Quote?> GetRandomQuoteAsync(CancellationToken cancellationToken = default)
        {
            RequestCount++;
            return Task.FromResult(quote);
        }

        public Task<IReadOnlyList<string>> GetMonacoThemesAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<JsTransform>> GetFileTransformsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<JsTransform>>([]);
    }
}
