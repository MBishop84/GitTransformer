using Microsoft.JSInterop;

namespace GitTransformer;

public class AppData
{
    public event Action? OnChange;
    private void NotifyDataChanged() => OnChange?.Invoke();
    public string SiteTheme { get; set { field = value ?? "default"; NotifyDataChanged(); } } = "default";
    public string MonacoTheme { get; set { field = value ?? "vs-dark"; NotifyDataChanged(); } } = "vs-dark";
    public int WindowWidth { get; set { field = value; NotifyDataChanged(); } }
    public int WindowHeight { get; set { field = value; NotifyDataChanged(); } }
    public bool IsLoaded { get; private set; } = false;

    public async Task LoadAsync(IJSRuntime js)
    {
        await js.InvokeVoidAsync("SetScrollEvent");
        SiteTheme = await js.InvokeAsync<string>("GetSetTheme") ?? SiteTheme;
        MonacoTheme = await js.InvokeAsync<string>("GetMonacoTheme") ?? MonacoTheme;
        WindowWidth = await js.InvokeAsync<int?>("GetWidth") ?? WindowWidth;
        WindowHeight = await js.InvokeAsync<int?>("GetHeight") ?? WindowHeight;
        if(WindowHeight > WindowWidth) await js.InvokeVoidAsync("HideFooter");
        IsLoaded = true;
    }
}