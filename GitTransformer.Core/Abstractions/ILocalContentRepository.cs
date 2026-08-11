using GitTransformer.Core.Models;

namespace GitTransformer.Core.Abstractions;

public interface ILocalContentRepository : IQuoteSource
{
    Task<IReadOnlyList<string>> GetMonacoThemesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<JsTransform>> GetFileTransformsAsync(CancellationToken cancellationToken = default);
}
