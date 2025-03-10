using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;
using System.Globalization;
using System.Text;
using System.Xml;

namespace GitTransformer.Pages;

public partial class Transformer
{
    internal class Bounds(string? Prefix = null, string? Suffix = null)
    {
        public string Prefix { get; set; } = Prefix ?? string.Empty;
        public string Suffix { get; set; } = Suffix ?? string.Empty;
    }

    #region Injected Services

    [Inject] private DialogService DialogService { get; init; } = null!;
    [Inject] private AppData AppData { get; set; } = null!;

    #endregion

    #region Properties

    record Literal(string ID)
    {
        public string ToolTip { get; set; } = ID;
        public string Default { get; set; } = ID;
    }
    private string UserCode { get; set; } = string.Empty;
    private Orientation Orientation { get; set; } = Orientation.Horizontal;

    #endregion

    #region Fields

    private readonly DialogOptions _dialogOptions = new()
    {
        Width = "max-content",
        Height = "max-content",
        Style = "max-width: 90vw; max-height: 90vh"
    };
    private Bounds _boundEach = new();
    private Bounds _boundAll = new();
    private bool _dynamic, _sort, _dupes, _openInModal;
    private string? _input, _output, _split, _join;
    private bool[] _selected = [true, false, false];

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
            return;
        try
        {
            if (AppData.WindowHeight > AppData.WindowWidth)
                Orientation = Orientation.Vertical;

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
                }, _dialogOptions);
        }
    }

    private async Task Transform()
    {
        try
        {
            ArgumentException.ThrowIfNullOrEmpty(_input);

            var split = _split?.Replace("\\n", "\n").Replace("\\t", "\t") ?? string.Empty;
            var join = _join?.Replace("\\n", "\n").Replace("\\t", "\t") ?? string.Empty;

            var outputArray = string.IsNullOrEmpty(split)
                ? _input?.ToCharArray().Select(x => SplitFunction(x.ToString())) ?? []
                : _input?.Split(split).Select(x => SplitFunction(x)) ?? [];

            if (_sort)
            {
                outputArray = [.. outputArray.OrderBy(x => x)];
            }
            if (_dupes)
            {
                outputArray = outputArray.Distinct();
            }

            _output = $"{_boundAll.Prefix}{string.Join(join, outputArray)}{_boundAll.Suffix}";
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "Transform Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                }, _dialogOptions);
        }
    }

    private string SplitFunction(string input)
        => _dynamic switch
        {
            true => int.TryParse(input, out var _)
                ? $"{input}"
                : $"{_boundEach.Prefix}{input}{_boundEach.Suffix}",
            false => $"{_boundEach.Prefix}{input}{_boundEach.Suffix}",
        };

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
            var lines = _input.Split("\n");
            var result = new StringBuilder();

            foreach (var line in lines)
            {
                var properties = line.Split("\t");
                result.Append($"///<summary>\n/// Gets/Sets the {properties[0]}.\n///</summary>\n");
                switch (properties.Length)
                {
                    case 1:
                        throw new ArgumentException("Insufficient arguments.\n\n");
                    case 2:
                        result.Append(properties[1] switch
                        {
                            var a when a.Contains("int", StringComparison.OrdinalIgnoreCase) =>
                                $"public int {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("date", StringComparison.OrdinalIgnoreCase) =>
                                $"public DateTime {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("bit", StringComparison.OrdinalIgnoreCase) =>
                                $"public bool {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("unique", StringComparison.OrdinalIgnoreCase) =>
                                $"public Guid {properties[0]} {{ get; set; }}\n\n",
                            _ => $"public string {properties[0]} {{ get; set; }}\n\n"
                        });
                        break;
                    case 3:
                        var isNullable = properties[2].Equals("YES", StringComparison.OrdinalIgnoreCase) ? "?" : "";
                        result.Append(properties[1] switch
                        {
                            var a when a.Contains("int", StringComparison.OrdinalIgnoreCase) =>
                                $"public int{isNullable} {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("date", StringComparison.OrdinalIgnoreCase) =>
                                $"public DateTime{isNullable} {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("bit", StringComparison.OrdinalIgnoreCase) =>
                                $"public bool{isNullable} {properties[0]} {{ get; set; }}\n\n",
                            var b when b.Contains("unique", StringComparison.OrdinalIgnoreCase) =>
                                $"public Guid{isNullable} {properties[0]} {{ get; set; }}\n\n",
                            _ => $"public string {properties[0]} {{ get; set; }}\n\n"
                        });
                        break;
                }
            }
            _output = result.ToString();
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "ClassFromQuery Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.ClassFromQuery },
                    { "Message", $"{ex.Message}\n\n{ex.StackTrace}" }
                }, _dialogOptions);
        }
    }

    private async Task JsonToRecords()
    {
        try
        {
            ArgumentNullException.ThrowIfNullOrEmpty(_input);

            List<string> records = [];

            var jsonObject = _input.StartsWith('[')
                ? JArray.Parse(_input)[0] as JObject
                : JObject.Parse(_input.Replace(" ", ""));

            ArgumentNullException.ThrowIfNullOrEmpty(jsonObject?.ToString());

            await DialogService.OpenAsync<CustomDialog>("Serializer",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.RecordsCheck },
                    { "Message", "Choose a serializer" }
                }, _dialogOptions);

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
                    records.AddRange(property.ProcessJProperty());
                else if (property.Value.Type == JTokenType.Array)
                    records.AddRange(property.ProcessJArray());
            }

            _output = string.Join("\n", records.Prepend($"""
                public record Root({Environment.NewLine}{string.Join(",\n", rootFields)});
                """));
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JsonToClass Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n{ex.StackTrace}" }
                }, _dialogOptions);
        }
    }

    private async Task XmlToClass()
    {
        try
        {
            if (string.IsNullOrEmpty(_input))
                throw new ArgumentException("Input is Empty");

            if (_input.Contains("&lt;") || _input.Contains("&gt;"))
                _input = _input.Replace("&lt;", "<").Replace("&gt;", ">");

            var xml = new XmlDocument();
            xml.LoadXml(_input);
            var result = new StringBuilder();
            var xmlRoot = xml.DocumentElement ?? throw new ArgumentException("XML must have a root element");
            result.Append(
                $"public class {xmlRoot.Name}\n{{\n");

            foreach (XmlNode node in xmlRoot.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Comment) continue;
                if (string.IsNullOrEmpty(node.InnerText) || node.ChildNodes.Count > 1)
                {
                    result.AppendFormat(
                        "\n\t///<summary>\n\t/// Gets/Sets the {0}.\n\t///</summary>\n\tpublic {1} {0} {{ get; set; }}\n",
                        node.Name,
                        $"{char.ToUpper(node.Name[0])}{node.Name[1..]}");
                }
                else
                {
                    result.AppendFormat(
                        "\n\t///<summary>\n\t/// Gets/Sets the {0}.\n\t///</summary>\n\tpublic {1} {0} {{ get; set; }} = {2};\n",
                        node.Name,
                        node.InnerText switch
                        {
                            var a when int.TryParse(a, out var _) => "int",
                            var b when DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _) => "DateTime",
                            var c when bool.TryParse(c, out var _) => "bool",
                            _ => "string"
                        },
                        node.InnerText switch
                        {
                            var a when int.TryParse(a, out var _) => "0",
                            var b when DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var _) => "DateTime.MinValue",
                            var c when bool.TryParse(c, out var _) => "false",
                            _ => "string.Empty"
                        });
                }
            }
            result.Append('}');
            _output = result.ToString();
        }
        catch (Exception ex)
        {
            var message = $"{ex.Message}\n{ex.StackTrace}";
            if (ex.Message.Contains("multiple root elements"))
                message = $"{ex.Message}\n\nPlease add an outer root element.";
            await DialogService.OpenAsync<CustomDialog>(
                "XmlToClass Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.XmlToClass },
                    { "Message", message }
                }, _dialogOptions);
        }
    }

    private async Task JsonToXML(string input)
    {
        try
        {
            var doc = JsonConvert.DeserializeXmlNode(input);
            using var sw = new StringWriter();
            await sw.WriteLineAsync("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            var writer = new XmlTextWriter(sw)
            {
                Formatting = System.Xml.Formatting.Indented
            };
            doc?.WriteContentTo(writer);
            _output = sw.ToString();
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "JsonToXML Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
                }, _dialogOptions);
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
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n\nExample Input Format:\n{Constants.SqlJsonFormat}" }
                }, _dialogOptions);
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
                        return p;

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
               new Dictionary<string, object>
               {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex}" }
               }, _dialogOptions);
        }
    }

    private async Task XmlToJson()
    {
        try
        {
            if (string.IsNullOrEmpty(_input))
                throw new ArgumentException("Input is Empty");
            var doc = new XmlDocument();
            doc.LoadXml(_input);
            _output = JsonConvert.SerializeObject(doc, Newtonsoft.Json.Formatting.Indented);
        }
        catch (Exception ex)
        {
            await DialogService.OpenAsync<CustomDialog>(
                "XmlToJson Error",
                new Dictionary<string, object>
                {
                    { "Type", Enums.DialogTypes.Error },
                    { "Message", $"{ex.Message}\n{ex.StackTrace}" }
                }, _dialogOptions);
        }
    }

    Task<dynamic> OpenInModalAsync<T>(
        string? title = "Dialog", Dictionary<string, object>? parameters = null) where T : ComponentBase
        => DialogService.OpenAsync<T>(
            title,
            parameters,
            new DialogOptions()
            {
                ShowClose = false,
                Resizable = true,
                Draggable = true,
                CloseDialogOnOverlayClick = true,
                Width = "80vw",
                Height = "80vh"
            });

    private void DialogClose(dynamic editorValue)
    {
        if (editorValue is string value)
            UserCode = value;

        _openInModal = false;
        StateHasChanged();
    }

    #endregion
}
