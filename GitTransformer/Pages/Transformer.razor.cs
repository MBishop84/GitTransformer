using BlazorMonaco.Editor;
using GitTransformer.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;
using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Xml;

namespace GitTransformer.Pages
{
    public partial class Transformer
    {
        internal class Bounds(string? Prefix = null, string? Suffix = null)
        {
            public string Prefix { get; set; } = Prefix ?? string.Empty;
            public string Suffix { get; set; } = Suffix ?? string.Empty;
        }

        #region Injected Services

        [Inject]
        private IJSRuntime JS { get; init; } = null!;
        [Inject]
        private DialogService DialogService { get; init; } = null!;
        [Inject]
        private LocalFileService FileClient { get; init; } = null!;
        [Inject]
        private AppData AppData { get; set; } = null!;

        #endregion

        #region Properties

        private string UserCode { get; set; } = string.Empty;
        private Orientation Orientation { get; set; } = Orientation.Horizontal;

        #endregion

        #region Fields

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

        /// <summary>
        /// Overrides the default behavior of the OnAfterRenderAsync method.
        /// </summary>
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
            }
        }

        /// <summary>
        /// Transforms the input string to the desired output based on variables.
        /// </summary>
        /// <returns>Sets the output</returns>
        /// <exception cref="ArgumentException"></exception>
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
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

        /// <summary>
        /// Clears the selected public field.
        /// </summary>
        /// <param name="field"></param>
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


        /// <summary>
        /// Converts the results of a SQL query to a C# class.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Shows the query to run to get the appropriate results for the input on error.
        /// </exception>
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
            }
        }

        /// <summary>
        /// Converts a JSON object to C# records.
        /// </summary>
        /// <remarks>Useful for making objects from api responses.</remarks>
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

                List<string> rootFields = [];
                foreach (var property in jsonObject.Properties())
                {
                    rootFields.Add(property.Value.Type switch
                    {
                        JTokenType.Object => $"""
                                [property: JsonPropertyName("{property.Name}")]
                                {property.Name.PascalCase()}? {property.Name.PascalCase()} = null
                            """,
                        JTokenType.Array => $"""
                                [property: JsonPropertyName("{property.Name}")]
                                {GetPropertyType(property)}? {property.Name.PascalCase().Plural()} = null
                            """,
                        _ => $"""
                                [property: JsonPropertyName("{property.Name}")]
                                {GetPropertyType(property)}? {property.Name.PascalCase()} = null
                            """
                    });
                    if (property.Value.Type == JTokenType.Object)
                        records.AddRange(ProcessProperty(property));
                    else if (property.Value.Type == JTokenType.Array)
                        records.AddRange(ProcessProperty(new JProperty(property.Name.PascalCase().Singular(), (property.Value as JArray)![0])));
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
            }
        }

        private static List<string> ProcessProperty(JProperty property)
        {
            List<string> records = [];
            var recordName = property.Name;
            List<string> fields = [];

            if (property.Value.Type == JTokenType.Object)
            {
                foreach (var child in property.Value.Children<JProperty>())
                {
                    fields.Add($"""
                            [property: JsonPropertyName("{child.Name}")]
                            {GetPropertyType(child)}? {child.Name.PascalCase()} = null
                        """);
                    if (child.Value.Type == JTokenType.Object)
                        records.AddRange(ProcessProperty(child));
                    else if (child.Value.Type == JTokenType.Array)
                        records.AddRange(ProcessProperty(new JProperty(child.Name.PascalCase().Singular(), (child.Value as JArray)![0])));
                }
            }
            else if (property.Value.Type == JTokenType.Array)
            {
                recordName = recordName.Singular();
                var first = new JProperty(property.Name.PascalCase().Singular(), (property.Value as JArray)![0]);
                records.AddRange(ProcessProperty(new JProperty(recordName, first.Value)));

                if (first.Value.Type == JTokenType.Object)
                {
                    foreach (var child in first.Value.Children<JProperty>())
                    {
                        fields.Add($"""
                                [property: JsonPropertyName("{child.Name}")]
                                {GetPropertyType(child)}? {child.Name.PascalCase()} = null
                            """);
                        if (child.Value.Type == JTokenType.Object)
                            records.AddRange(ProcessProperty(child));
                        else if (child.Value.Type == JTokenType.Array)
                            records.AddRange(ProcessProperty(new JProperty(child.Name.PascalCase().Singular(), (child.Value as JArray)![0])));
                    }
                }
                else
                {
                    fields.Add($"""
                            [property: JsonPropertyName("{property.Name}")]
                            IEnumerable<{GetPropertyType(new JProperty(recordName, first.Value))}>? {property.Name.PascalCase().Plural()} = null
                        """);
                }
            }
            else
            {
                fields.Add($"""
                        [property: JsonPropertyName("{property.Name}")]
                        {GetPropertyType(property)}? {property.Name.PascalCase()} = null
                    """);
            }

            records.Add($"public record {recordName.PascalCase()}(\n{string.Join(",\n", fields)});");

            return records;
        }

        private static string GetPropertyType(JProperty token)
            => token.Value.Type switch
            {
                JTokenType.Integer => "int",
                JTokenType.Float => "double",
                JTokenType.Boolean => "bool",
                JTokenType.Date => "DateTime",
                JTokenType.Bytes => "byte[]",
                JTokenType.Guid => "Guid",
                JTokenType.TimeSpan => "TimeSpan",
                JTokenType.Array => (token.Value as JArray)![0].Type switch
                {
                    JTokenType.Object => $"IEnumerable<{token.Path.Split(".")[^1].PascalCase().Singular()}>",
                    _ => $"IEnumerable<{GetPropertyType(new JProperty(token.Name.PascalCase().Singular(), (token.Value as JArray)![0]))}>",
                },
                JTokenType.Object => token.Name.PascalCase(),
                _ => "string"
            };

        /// <summary>
        /// Converts XML to a C# class.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Error dialog displays the error with an example of what this function works best with.
        /// </exception>
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
            }
        }

        /// <summary>
        /// Converts a JSON object to XML.
        /// </summary>
        /// <returns></returns>
        private async Task JsonToXML()
        {
            try
            {
                if (string.IsNullOrEmpty(_input))
                    throw new ArgumentException("Input is Empty");
                if (!_input.StartsWith('{'))
                    _input = $"{{{_input}}}";
                var doc = JsonConvert.DeserializeXmlNode("{\"DefaultRoot\":" + $"{_input}}}");
                using var sw = new StringWriter();
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
                        { "Message", $"{ex.Message}\n{ex.StackTrace}" }
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh",
                        Style = "max-width: 90vw;"
                    });
            }
        }

        /// <summary>
        /// Converts XML to a C# class.
        /// </summary>
        /// <remarks>Not yet Implemented</remarks>
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
                    },
                    new DialogOptions()
                    {
                        Width = "max-content",
                        Height = "50vh"
                    });
            }
        }

        Task<dynamic> OpenInModalAsync<T>(
            string? title = "Dialog",
            Dictionary<string, object>? parameters = null)
            where T : ComponentBase =>
            DialogService.OpenAsync<T>(
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
}
