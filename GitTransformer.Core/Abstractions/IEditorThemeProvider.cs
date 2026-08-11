namespace GitTransformer.Core.Abstractions;

public interface IEditorThemeProvider<TTheme>
{
    Task<TTheme?> GetThemeAsync(string theme, CancellationToken cancellationToken = default);
}
