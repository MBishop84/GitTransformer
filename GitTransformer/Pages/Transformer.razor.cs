using GitTransformer.Pages.Components;
using GitTransformer.Application.Abstractions;
using GitTransformer.Core.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;

namespace GitTransformer.Pages;

public partial class Transformer
{
    internal class Bounds(string? Prefix = null, string? Suffix = null)
    {
        public string Prefix { get; set; } = Prefix ?? string.Empty;
        public string Suffix { get; set; } = Suffix ?? string.Empty;
    }

    #region Injected Services

    [Inject] private IJSRuntime JS { get; init; } = null!;
    [Inject] private DialogService DialogService { get; init; } = null!;
    [Inject] private AppData AppData { get; set; } = null!;
    [Inject] private ITextTransformationService TransformationService { get; init; } = null!;

    #endregion

    #region Properties

    private record Literal(string ID)
    {
        public string ToolTip { get; set; } = ID;
        public string Default { get; set; } = ID;
    }
    private string UserCode { get; set; } = string.Empty;
    private Orientation Orientation { get; set; } = Orientation.Horizontal;

    #endregion

    #region Fields

    private Bounds _boundEach = new();
    private Bounds _boundAll = new();
    private bool _dynamic, _sort, _dupes, _openInModal;
    private string? _input, _output, _split, _join;
    private readonly bool[] _selected = [true, false, false];

    #endregion

    #region Methods

    protected override void OnInitialized()
    {
        AppData.OnChange += StateHasChanged;
        DialogService.OnClose += DialogClose;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        try
        {
            if (AppData.WindowHeight > AppData.WindowWidth)
            {
                Orientation = Orientation.Vertical;
            }

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
                }, Constants.DialogOptions);
        }
    }

    private async Task Transform()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_input);
            _output = TransformationService.Transform(
                _input,
                new TextTransformOptions(
                    _split,
                    _join,
                    new TextBounds(_boundAll.Prefix, _boundAll.Suffix),
                    new TextBounds(_boundEach.Prefix, _boundEach.Suffix),
                    _dynamic,
                    _sort,
                    _dupes));
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "Transform Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                }, Constants.DialogOptions);
        }
    }

    private void ClearField(string field)
    {
        switch (field)
        {
            case nameof(_input):
                _input = null;
                break;
            case nameof(_output):
                _output = null;
                break;
            case nameof(_split):
                _split = null;
                break;
            case nameof(_join):
                _join = null;
                break;
            case nameof(_boundAll):
                _boundAll = new();
                break;
            case nameof(_boundEach):
                _boundEach = new();
                break;
            default:
                break;
        }
        StateHasChanged();
    }

    private async Task ClassFromQuery()
    {
        try
        {
            if (string.IsNullOrEmpty(_input))
            {
                throw new ArgumentException("Input is Empty");
            }
            var comments = await DialogService.Confirm(
                "Include Comments?",
                "Comments",
                new ConfirmOptions() { OkButtonText = "Yes", CancelButtonText = "No" }) ?? false;

            _output = TransformationService.ClassFromQuery(_input, comments);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "ClassFromQuery Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.ClassFromQuery },
                    { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                }, Constants.DialogOptions);
        }
    }

    private async Task JsonToRecords()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_input);

            List<string> records = [];

            var jsonObject = _input.StartsWith('[')
                ? JArray.Parse(_input)[0] as JObject
                : JObject.Parse(_input.Replace(" ", ""));

            ArgumentException.ThrowIfNullOrEmpty(jsonObject?.ToString());

            await DialogService.OpenAsync<CustomDialog>("Serializer",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.RecordsCheck },
                    { "Message", "Choose a serializer" }
                }, Constants.DialogOptions);

            List<string> rootFields = [];
            foreach (var property in jsonObject.Properties())
            {
                rootFields.Add(property.Value.Type switch
                {
                    JTokenType.Object => $"""
                            {property.Name.GetDecorator()}{property.Name.PascalCase()}? {property.Name.PascalCase()} = null
                        """,
                    JTokenType.Array => $"""
                            {property.Name.GetDecorator()}{property.GetJPropertyType()}? {property.Name.PascalCase().Plural()} = null
                        """,
                    _ => $"""
                            {property.Name.GetDecorator()}{property.GetJPropertyType()}? {property.Name.PascalCase()} = null
                        """
                });
                if (property.Value.Type == JTokenType.Object)
                {
                    records.AddRange(property.ProcessJProperty());
                }
                else if (property.Value.Type == JTokenType.Array)
                {
                    records.AddRange(property.ProcessJArray());
                }
            }
            records = [.. records.Distinct()];
            _output = string.Join("\n", records.Prepend($"""
                public record Root({Environment.NewLine}{string.Join(",\n", rootFields)});
                """));
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JsonToClass Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n{ex.StackTrace}" }
                }, Constants.DialogOptions);
        }
    }

    private async Task XmlToClass()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_input);
            _output = TransformationService.XmlToClass(_input);
        }
        catch (Exception ex)
        {
            var message = $"{ex.Message}\n{ex.StackTrace}";
            if (ex.Message.Contains("multiple root elements"))
            {
                message = $"{ex.Message}\n\nPlease add an outer root element.";
            }

            await DialogService.OpenAsync<CustomDialog>(
                "XmlToClass Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.XmlToClass },
                    { "Message", message }
                }, Constants.DialogOptions);
        }
    }

    private async Task JsonToXML(string input)
    {
        try
        {
            _output = TransformationService.JsonToXml(input);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JsonToXML Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
                }, Constants.DialogOptions);
        }
    }

    private async Task SQLJsonToSnippet()
    {
        try
        {
            ArgumentNullException.ThrowIfNullOrEmpty(_input);

            var formattedJson = ReformatJsonForSnippet(JObject.Parse(_input));
            await JsonToXML(formattedJson);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "SQLJsonToSnippet",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n\nExample Input Format:\n{Constants.SqlJsonFormat}" }
                }, Constants.DialogOptions);
        }
    }

    private static string ReformatJsonForSnippet(JObject input)
    {
        var template = JObject.Parse(Constants.SnippetTemplate);
        var title = input["prefix"]?.ToString() ?? "Title";
        var description = input["description"]?.ToString() ?? "Description";
        var codeArray = (input["body"] as JArray)
            ?.Select(x => $"{x}".Replace("{", "").Replace("}", "$"));
        ArgumentNullException.ThrowIfNull(codeArray);

        template["CodeSnippets"]!["CodeSnippet"]!["Header"]!["Title"] = title.PascalCase();
        template["CodeSnippets"]!["CodeSnippet"]!["Header"]!["Description"] = description;
        if (codeArray.Any(x => x.Contains('$')))
        {
            codeArray = codeArray
                !.Select(p =>
                {
                    if (!p.Contains(':'))
                    {
                        return p;
                    }

                    var parts = p.Split('$') ?? [];
                    foreach (var part in parts.Select((v, i) => new { v, i }))
                    {
                        parts[part.i] = part.v.Contains(':')
                            ? part.v.Split(':')[1]
                            : part.v;
                    }

                    return string.Join('$', parts);
                });

            template["CodeSnippets"]!["CodeSnippet"]!["Snippet"]!["Declarations"]!["Literal"] =
                JArray.Parse(JsonConvert.SerializeObject(
                    codeArray.Where(x => x.Contains('$'))
                    .SelectMany(a => a.Split('$').Select((v, i) => new { v, i })
                    .Where(z => z.i % 2 != 0).Select(p => new Literal(p.v)).Distinct())));
        }
        template["CodeSnippets"]!["CodeSnippet"]!["Snippet"]!["Code"]!["#cdata-section"] = $"\n{string.Join("\n", codeArray)}\n";

        return template.ToString();
    }

    private async Task DotSnippet()
    {
        try
        {
            var template = JObject.Parse(Constants.SnippetTemplate);
            var code = string.IsNullOrEmpty(_input)
                ? "<!-- Code Goes Here -->"
                : _input;
            template["CodeSnippets"]!["CodeSnippet"]!["Snippet"]!["Code"]!["#cdata-section"] = $"\n{code}\n";

            await JsonToXML(template.ToString());
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
               "SQLJsonToSnippet",
               new Dictionary<string, object?>
               {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
               }, Constants.DialogOptions);
        }
    }

    private async Task XmlToJson()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_input);
            _output = TransformationService.XmlToJson(_input);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "XmlToJson Error",
                new Dictionary<string, object?>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
                }, Constants.DialogOptions);
        }
    }

    private void DialogClose(dynamic editorValue)
    {
        if (editorValue is string value)
        {
            UserCode = value;
        }

        _openInModal = false;
        StateHasChanged();
    }

    #endregion
}
