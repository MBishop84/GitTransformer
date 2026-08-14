using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;
using Microsoft.JSInterop;

namespace GitTransformer.Browser;

public sealed class IndexedDbJsTransformStore(IJSRuntime jsRuntime) : IJsTransformStore, IAsyncDisposable
{
    private Task<IJSObjectReference>? _moduleTask;

    public async Task<IReadOnlyList<JsTransform>> GetAllAsync(
        IReadOnlyCollection<JsTransform> defaultTransforms,
        CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<JsTransform[]>(
            "getAll",
            cancellationToken,
            defaultTransforms) ?? [];
    }

    public async Task UpsertAsync(JsTransform transform, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("upsert", cancellationToken, transform);
    }

    public async Task DeleteAsync(string name, CancellationToken cancellationToken = default)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("remove", cancellationToken, name);
    }

    public async ValueTask DisposeAsync()
    {
        if (_moduleTask is not null)
        {
            var module = await _moduleTask;
            await module.DisposeAsync();
        }
    }

    private Task<IJSObjectReference> GetModuleAsync()
        => _moduleTask ??= jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./js/jsTransformStore.js").AsTask();
}
