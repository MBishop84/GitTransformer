namespace GitTransformer;

public class AppData
{
    public event Action? OnChange;
    private void NotifyDataChanged() => OnChange?.Invoke();
    public string SiteTheme { get; set { field = value ?? "default"; NotifyDataChanged(); } } = "default";
    public string MonacoTheme { get; set { field = value ?? "vs-dark"; NotifyDataChanged(); } } = "vs-dark";
    public int WindowWidth { get; set { field = value; NotifyDataChanged(); } }
    public int WindowHeight { get; set { field = value; NotifyDataChanged(); } }
}