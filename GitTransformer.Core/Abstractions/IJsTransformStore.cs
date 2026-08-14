using GitTransformer.Core.Models;

namespace GitTransformer.Core.Abstractions;

public interface IJsTransformStore
{
    Task<IReadOnlyList<JsTransform>> GetAllAsync(
        IReadOnlyCollection<JsTransform> defaultTransforms,
        CancellationToken cancellationToken = default);

    Task UpsertAsync(JsTransform transform, CancellationToken cancellationToken = default);

    Task DeleteAsync(string name, CancellationToken cancellationToken = default);
}
