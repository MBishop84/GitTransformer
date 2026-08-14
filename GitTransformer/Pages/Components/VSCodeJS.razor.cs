using BlazorMonaco.Editor;
using GitTransformer.Core.Abstractions;
using GitTransformer.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;
using Radzen.Blazor;
using System.Security.Cryptography;
using System.Text;

namespace GitTransformer.Pages.Components;

public partial class VSCodeJS : IAsyncDisposable
{
    [Inject] AppData AppData { get; set; } = null!;
    [Inject] IJSRuntime JS { get; init; } = null!;
    [Inject] DialogService DialogService { get; init; } = null!;
    [Inject] ILocalContentRepository LocalContent { get; init; } = null!;
    [Inject] IJsTransformStore JsTransformStore { get; init; } = null!;
    [Inject] IEditorThemeProvider<StandaloneThemeData> ThemeProvider { get; init; } = null!;
    [Parameter] public string Output { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> OutputChanged { get; set; }
    [Parameter] public string UserCode { get; set; } = string.Empty;
    [Parameter] public EventCallback<string> UserCodeChanged { get; set; }
    [Parameter] public bool InModal { get; set; } = false;
    StandaloneCodeEditor Editor { get; set; } = null!;

    List<string> _monacoThemes = [];
    List<JsTransform?> _jsTransforms = [];
    readonly string[] _defaultThemes = ["vs-dark", "vs-light"];
    string? _entry;
    bool _isDisposed;
    bool _isDebug;

    protected override void OnInitialized()
    {
        AppData.OnChange += StateHasChanged;
        DialogService.OnClose += DialogClose;
#if DEBUG
        _isDebug = true;
#endif
    }

    protected override async Task OnInitializedAsync()
    {
        var themes = await LocalContent.GetMonacoThemesAsync();
        _monacoThemes = [.. themes
            .Concat(_defaultThemes)
            .Distinct(StringComparer.Ordinal)];
        _jsTransforms = [.. await LocalContent.GetFileTransformsAsync()];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;
        try
        {
            var storedTransforms = await JsTransformStore.GetAllAsync(_jsTransforms.OfType<JsTransform>().ToArray());
            foreach (var transform in storedTransforms)
            {
                var existingIndex = _jsTransforms.FindIndex(item => item?.Name == transform.Name);
                if (existingIndex >= 0)
                    _jsTransforms[existingIndex] = transform;
                else
                    _jsTransforms.Add(transform);
            }

            await ChangeTheme(AppData.MonacoTheme);

            if (!string.IsNullOrEmpty(UserCode))
                await Editor.SetValue(UserCode);

            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "OnAfterRenderAsync Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private static StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
    {
        return new StandaloneEditorConstructionOptions
        {
            AutomaticLayout = true,
            Language = "javascript",
            Value = "//StarterCode\noutput = input;",
            TabSize = 2,
            DetectIndentation = true,
            TrimAutoWhitespace = true,
            WordBasedSuggestionsOnlySameLanguage = true,
            StablePeek = true
        };
    }

    private async Task JavaScript(RadzenSplitButtonItem item)
    {
        if (item?.Text == "Save")
        {
            await SaveJs();
            return;
        }
        if (item?.Text == "Delete")
        {
            await DeleteJs();
            return;
        }
        try
        {
            var userCode = await Editor.GetValue();

            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("Please enter or choose a function.");
            if (!userCode.Contains("output"))
                throw new ArgumentException("Please assign a value to output");
            if (!userCode.Contains("input"))
                throw new ArgumentException("You must use the input.");

            Output = await JS.InvokeAsync<string>("RunUserScript", userCode);
            await OutputChanged.InvokeAsync(Output);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JavaScript Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private async Task SaveJs()
    {
        try
        {
            var userCode = await Editor.GetValue();
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("Code is Empty");
            var name = userCode.Split("\n")[0];
            if (!await DialogService.Confirm(
                $"Is {name} the name for your transform?",
                "Confirmation",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" }) ?? false)
            {
                await DialogService.OpenAsync<CustomDialog>(
                    "Enter Transform Name",
                    new Dictionary<string, object?>
                    {
                        { "Type", Enums.DialogTypes.Text },
                        { "Message", "Please name your transform." }
                    },
                    new DialogOptions() { Width = "max-content", Height = "200px" });
                if (string.IsNullOrEmpty(_entry))
                    throw new ArgumentException("Transform Name is Empty");
                name = _entry.StartsWith("//") ? _entry : $"//{_entry}";
                _entry = null;
            }
            else { userCode = userCode.Replace($"{name}\n", ""); }

            await DialogService.OpenAsync<CustomDialog>(
                "Enter Name",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Text },
                    { "Message", "Please enter your name to take ownership of this transform." }
                },
                new DialogOptions() { Width = "max-content", Height = "200px" });

            if (string.IsNullOrEmpty(_entry))
                throw new ArgumentException("Name is Empty");

            var newTransform = new JsTransform(0, _entry, name, userCode);
            await JsTransformStore.UpsertAsync(newTransform);
            var existingIndex = _jsTransforms.FindIndex(transform => transform?.Name == name);
            if (existingIndex >= 0)
                _jsTransforms[existingIndex] = newTransform;
            else
                _jsTransforms.Add(newTransform);

            if (_isDebug) await DownloadJsTransformsJson();

            await DialogService.Alert("Transform Added!", "Success");
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
            "SaveJs Error",
            new Dictionary<string, object?>
            {
                { "Type", Enums.DialogTypes.Error },
                { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
            },
            new DialogOptions()
            {
                Width = "max-content",
                Height = "50vh"
            });
        }
    }

    private async Task DeleteJs()
    {
        try
        {
            var userCode = await Editor.GetValue();
            var name = userCode.Split("\n")[0];
            var jsTransform = _jsTransforms.Find(x => x?.Name == name);
            var deleteMessage = $"{name} has been deleted from this instance.";
            if (jsTransform == null)
                throw new ArgumentException("No code found to delete.");

            if (await DialogService.Confirm(
                $"Are you sure you want to delete {name}?",
                "Final Confirmation",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" }) ?? false)
            {
                await JsTransformStore.DeleteAsync(jsTransform.Name);
                _jsTransforms.Remove(jsTransform);

                if (_isDebug) await DownloadJsTransformsJson();

                await Editor.SetValue(string.Empty);
                await InvokeAsync(StateHasChanged);
                await DialogService.Alert(deleteMessage, "Success!");
            }
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "DeleteJs Error",
                new Dictionary<string, object?>
                {
                { "Type", Enums.DialogTypes.Error },
                { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private async Task PreviousJs()
    {
        try
        {
            var userCode = await Editor.GetValue();
            if (string.IsNullOrEmpty(userCode))
                userCode = $"{_jsTransforms[0]?.Name}\n{_jsTransforms[0]?.Code}";
            else
            {
                var index = _jsTransforms.FindIndex(x => x?.Name == userCode.Split("\n")[0]);
                if (index > 0)
                    userCode = $"{_jsTransforms[index - 1]?.Name}\n{_jsTransforms[index - 1]?.Code}";
                else
                    userCode = $"{_jsTransforms[^1]?.Name}\n{_jsTransforms[^1]?.Code}";
            }
            await Editor.SetValue(userCode);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "PreviousJs Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private async Task NextJs()
    {
        try
        {
            var userCode = await Editor.GetValue();
            if (string.IsNullOrEmpty(userCode))
                userCode = $"{_jsTransforms[0]?.Name} \n {_jsTransforms[0]?.Code}";
            else
            {
                var index = _jsTransforms.FindIndex(x => x?.Name == userCode.Split("\n")[0]);
                userCode = index < _jsTransforms.Count - 1
                    ? $"{_jsTransforms[index + 1]?.Name}\n{_jsTransforms[index + 1]?.Code}"
                    : $"{_jsTransforms[0]?.Name}\n{_jsTransforms[0]?.Code}";
            }
            await Editor.SetValue(userCode);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "NextJs Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n{ex.StackTrace}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private Task UpdateUserCode(string jsTransform)
    {
        var fullString = _jsTransforms.Where(x => x?.Code == jsTransform)
            .Select(y => $"{y?.Name}\n{y?.Code}").FirstOrDefault();
        return Editor?.SetValue(fullString) ?? Task.CompletedTask;
    }

    private async Task ChangeTheme(string theme)
    {
        try
        {
            var myTheme = theme.Replace(" ", "");
            if (!_defaultThemes.Contains(theme))
            {
                await Global.DefineTheme(JS, myTheme,
                    await ThemeProvider.GetThemeAsync(theme));
            }

            await Global.SetTheme(JS, myTheme);
            if (AppData.MonacoTheme == theme)
                return;

            AppData.MonacoTheme = theme;
            await JS.InvokeVoidAsync("localStorage.setItem", "MonacoTheme", theme);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "MonacoTheme Error",
                new Dictionary<string, object?>
                {
                { "Type", Enums.DialogTypes.Error },
                { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                },
                new DialogOptions()
                {
                    Width = "max-content",
                    Height = "50vh"
                });
        }
    }

    private void DialogClose(dynamic entry)
    {
        if (entry != null)
            _entry = $"{entry}";

        StateHasChanged();
    }

    public async ValueTask DisposeAsync()
    {
        UserCode = await Editor.GetValue();
        await UserCodeChanged.InvokeAsync(UserCode);
        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        await Task.Yield();
        if (!_isDisposed)
        {
            if (disposing)
            {
                AppData.OnChange -= StateHasChanged;
                DialogService.OnClose -= DialogClose;
            }
            _isDisposed = true;
        }
    }

    async Task DownloadJsTransformsJson()
    {
        if (!_isDebug) return;

        var json = JsonConvert.SerializeObject(
            _jsTransforms.OfType<JsTransform>(),
            Formatting.Indented);

        await JS.InvokeVoidAsync(
            "downloadTextFile",
            "JsTransforms.json",
            json,
            "application/json");
    }
}
