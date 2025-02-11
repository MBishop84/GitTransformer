using BlazorMonaco.Editor;
using GitTransformer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using Radzen.Blazor;
using System.Security.Cryptography;
using System.Text;

namespace GitTransformer.Pages.Components;

public partial class VSCodeJS
{
    [Inject]
    private AppData AppData { get; set; } = null!;
    [Inject]
    private IJSRuntime JS { get; init; } = null!;
    [Inject]
    private DialogService DialogService { get; init; } = null!;
    [Inject]
    private LocalFileService FileClient { get; init; } = null!;
    private StandaloneCodeEditor Editor { get; set; } = null!;
    private List<string> _monacoThemes = [];
    private List<JsTransform?> _jsTransforms = [];
    private readonly string[] _defaultThemes = ["vs-dark", "vs-light"];
    private string? _entry;

    protected override void OnInitialized()
    {
        AppData.OnChange += StateHasChanged;
        DialogService.OnClose += DialogClose;
    }

    protected override async Task OnInitializedAsync()
    {
        _monacoThemes = await FileClient.GetMonacoThemes();
        _monacoThemes.AddRange(_defaultThemes);
        _jsTransforms = await FileClient.GetFileTransforms();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {

            var localTransforms = await JS.InvokeAsync<string?>("localStorage.getItem", "JsTransforms");

            if (!string.IsNullOrEmpty(localTransforms))
            {
                var items = JsonConvert.DeserializeObject<List<JsTransform>>(localTransforms)!;
                _jsTransforms.AddRange(items.Where(x => !_jsTransforms.Select(y => y?.Name).Contains(x.Name)));
            }

            await ChangeTheme(AppData.MonacoTheme);
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "OnAfterRenderAsync Error",
                new Dictionary<string, object>
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
        if (item?.Text == "1")
        {
            await SaveJs();
            return;
        }
        if (item?.Text == "2")
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

            await JS.InvokeVoidAsync("RunUserScript", userCode);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JavaScript Error",
                new Dictionary<string, object>
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

    private async Task SaveJs()
    {
        try
        {
            var userCode = await Editor.GetValue();
            if (string.IsNullOrEmpty(userCode))
                throw new ArgumentException("Input is Empty");
            var name = userCode.Split("\n")[0];
            if (!await DialogService.Confirm(
                $"Is {name} the name for your transform?",
                "Confirmation",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" }) ?? false)
            {
                await DialogService.OpenAsync<CustomDialog>(
                    "Enter Transform Name",
                    new Dictionary<string, object>
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
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Text },
                    { "Message", "Please enter your name to take ownership of this transform." }
                },
                new DialogOptions() { Width = "max-content", Height = "200px" });

            if (string.IsNullOrEmpty(_entry))
                throw new ArgumentException("Name is Empty");

            if (_jsTransforms.Exists(x => x?.Name == name))
                _jsTransforms.Remove(_jsTransforms.Find(x => x?.Name == name)!);

            var newTransform = new JsTransform(0, _entry, name, userCode);
            _jsTransforms.Add(newTransform);
            await JS.InvokeAsync<string>("localStorage.setItem", "JsTransforms", JsonConvert.SerializeObject(_jsTransforms));
            await InvokeAsync(StateHasChanged);
            await DialogService.Alert(
                JsonConvert.SerializeObject(newTransform, Newtonsoft.Json.Formatting.Indented),
                "Transform Added!");
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
            "SaveJs Error",
            new Dictionary<string, object>
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
                await DialogService.OpenAsync<CustomDialog>("Password", new Dictionary<string, object>
            {
                { "Type", Enums.DialogTypes.Password },
                { "Message", "Please enter your key to permanently delete this code." }
            }, new DialogOptions() { Width = "max-content", Height = "200px" });

                if (string.IsNullOrEmpty(_entry))
                    throw new ArgumentException("Password is Empty");
                if ("+aGrr99mKAuTRZp/t0aSzvD6vSHtr0nNv4NFTVuxTH0="
                    !.Equals(Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(_entry)))))
                {
                    deleteMessage = $"{name} has been permanently deleted.";
                }
                _jsTransforms.Remove(jsTransform);
                await JS.InvokeAsync<string>("localStorage.setItem", "JsTransforms", _jsTransforms);
                await Editor.SetValue(string.Empty);
                await InvokeAsync(StateHasChanged);
                await DialogService.Alert(deleteMessage, "Success!");
            }
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "DeleteJs Error",
                new Dictionary<string, object>
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
                new Dictionary<string, object>
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
                new Dictionary<string, object>
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
        return Editor.SetValue(fullString);
    }

    private async Task ChangeTheme(string theme)
    {
        try
        {
            var myTheme = theme.Replace(" ", "");
            if (!_defaultThemes.Contains(theme))
            {
                await Global.DefineTheme(JS, myTheme,
                    await FileClient.GetStandaloneThemeData(theme));
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
                new Dictionary<string, object>
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
        if (entry == null)
            return;

        _entry = $"{entry}";
        StateHasChanged();
    }
}
