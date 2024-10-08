﻿using BlazorMonaco.Editor;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Radzen;
using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;

namespace GitTransformer.Pages
{
    public partial class Transformer
    {
        #region Injected Services

        [Inject]
        private IJSRuntime JS { get; init; } = null!;
        [Inject]
        private DialogService DialogService { get; init; } = null!;
        [Inject]
        private HttpClient ApiClient { get; init; } = null!;
        [Inject]
        private IConfiguration Configuration { get; init; } = null!;

        #endregion

        #region Properties

        private sealed record JsTransform(int Id, string AddedBy, string Name, string Code);
        private StandaloneCodeEditor Editor { get; set; } = null!;
        private Orientation Orientation { get; set; } = Orientation.Horizontal;

        #endregion

        #region Feilds

        private List<string> _monacoThemes = [];
        private List<JsTransform> _jsTransforms = [];
        private int _height = 1000, _width = 1000;
        private bool _dynamic, _sort, _dupes;
        private IEnumerable<int> _options = [];
        private string? _input, _output, _split, _join, _boundAll, _boundEach, _monacoTheme, _entry;

        #endregion

        #region Methods

        /// <summary>
        /// Overrides the default behavior of OnInitializedAsync
        /// </summary>
        /// <returns></returns>
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            try
            {
                _monacoThemes = (await ApiClient.GetFromJsonAsync<Dictionary<string, string>>("themes/themelist.json") ?? [])
                    .Select(x => x.Value).ToList();
                _monacoThemes.AddRange(["vs-dark", "vs-light"]);
                DialogService.OnClose += DialogClose;
            }
            catch (Exception ex)
            {
                _input = ex.Message;
                _output = ex.Message;
            }
        }

        /// <summary>
        /// Overrides the default behavior of the OnAfterRenderAsync method.
        /// </summary>
        /// <param name="firstRender"></param>
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                return;

            _height = await JS.InvokeAsync<int>("GetHeight");
            _width = await JS.InvokeAsync<int>("GetWidth");
            if (_height > _width)
                Orientation = Orientation.Vertical;

            var theme = await JS.InvokeAsync<string>("localStorage.getItem", "MonacoTheme");
            if (string.IsNullOrEmpty(theme))
            {
                await ChangeTheme("vs-dark");
            }
            else
            {
                await ChangeTheme(theme);
            }
            _jsTransforms = await ApiClient.GetFromJsonAsync<List<JsTransform>>("data/JsTransforms.json") ?? [];
            var localTransforms = await JS.InvokeAsync<string>("localStorage.getItem", "JsTransforms");
            if (!string.IsNullOrEmpty(localTransforms))
            {
                var items = JsonConvert.DeserializeObject<List<JsTransform>>(localTransforms)!;
                _jsTransforms.AddRange(items.Where(x => !_jsTransforms.Select(y => y.Name).Contains(x.Name)));
            }

            await InvokeAsync(StateHasChanged);
        }

        private void DialogClose(dynamic entry)
        {
            if (entry == null)
                return;

            _entry = $"{entry}";
            StateHasChanged();
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
                if (string.IsNullOrEmpty(_input))
                    throw new ArgumentException("Input is Empty");
                if (!string.IsNullOrEmpty(_boundAll) && _boundAll.Split('.').Length != 2)
                    throw new ArgumentException("Bound-All must be separated by a period(.)");
                if (!string.IsNullOrEmpty(_boundEach) && _boundEach.Split('.').Length != 2)
                    throw new ArgumentException("Bound-Each must be separated by a period(.)");
                var split = _split?.Replace("\\n", "\n").Replace("\\t", "\t") ?? string.Empty;
                var join = _join?.Replace("\\n", "\n").Replace("\\t", "\t") ?? string.Empty;

                var frontBracket = string.IsNullOrEmpty(_boundAll)
                    ? string.Empty
                    : _boundAll.Split('.')[0];
                var endBracket = string.IsNullOrEmpty(_boundAll)
                    ? string.Empty
                    : _boundAll.Split('.')[1];
                var frontParentheses = string.IsNullOrEmpty(_boundEach)
                    ? string.Empty
                    : _boundEach.Split('.')[0];
                var endParentheses = string.IsNullOrEmpty(_boundEach)
                    ? string.Empty
                    : _boundEach.Split('.')[1];

                var outputArray = string.IsNullOrEmpty(split)
                    ? _input?.ToCharArray().Select(x =>
                        {
                            return _dynamic switch
                            {
                                true => int.TryParse(x.ToString(), out var i)
                                    ? $"{x}"
                                    : $"{frontParentheses}{x}{endParentheses}",
                                false => $"{frontParentheses}{x}{endParentheses}"
                            };
                        }) ?? []
                    : _input?.Split(split).Select(x =>
                        {
                            return _dynamic switch
                            {
                                true => int.TryParse(x, out var i) || x.Equals("null", StringComparison.OrdinalIgnoreCase)
                                    ? $"{x}"
                                    : $"{frontParentheses}{x}{endParentheses}",
                                false => $"{frontParentheses}{x}{endParentheses}"
                            };
                        }) ?? [];

                if (_options.Any(x => x == 1))
                {
                    outputArray = outputArray.OrderBy(x => x).ToImmutableList();
                }
                if (_options.Any(x => x == 2))
                {
                    outputArray = outputArray.Distinct().ToImmutableList();
                }

                _output = $"{frontBracket}{string.Join(join, outputArray)}{endBracket}";
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
                    _boundAll = null;
                    break;
                case nameof(_boundEach):
                    _boundEach = null;
                    break;
                case nameof(_monacoTheme):
                    _monacoTheme = null;
                    break;
                case nameof(_entry):
                    _entry = null;
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
        /// Converts a JSON object to a C# class.
        /// </summary>
        /// <remarks>Useful for making objects from api responses.</remarks>
        /// <exception cref="ArgumentException"></exception>
        private async Task JsonToClass()
        {
            try
            {
                if (string.IsNullOrEmpty(_input))
                    throw new ArgumentException("Input is Empty");
                if (!_input.StartsWith('{'))
                    _input = $"{{{_input}}}";
                dynamic? jsonObject = System.Text.Json.JsonSerializer.Deserialize<dynamic>(_input);
                if (jsonObject is null) return;
                var result = new StringBuilder();
                foreach (JsonProperty i in jsonObject.EnumerateObject())
                {
                    var name = char.ToUpper(i.Name[0]) + i.Name.Substring(1);
                    result.AppendFormat("///<summary>\n/// Gets/Sets the {0}.\n///</summary>\n", name);
                    result.Append(i.Value.ValueKind switch
                    {
                        JsonValueKind.Number =>
                            $"public int {name} {{ get; set; }}\n\n",
                        JsonValueKind.String => i.Value switch
                        {
                            var b when DateTime.TryParse(b.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var itemDate) =>
                                $"public DateTime? {name} {{ get; set; }}\n\n",
                            _ => $"public string {name} {{ get; set; }} = string.Empty;\n\n"
                        },
                        JsonValueKind.True or JsonValueKind.False =>
                            $"public bool {name} {{ get; set; }}\n\n",
                        JsonValueKind.Array =>
                            $"public ICollection<{name}> {name} {{ get; set; }}\n\n",
                        JsonValueKind.Object =>
                            $"public {name} {name} {{ get; set; }}\n\n",
                        JsonValueKind.Undefined => string.Empty,
                        JsonValueKind.Null => string.Empty,
                        _ => string.Empty
                    });
                }
                _output = result.ToString();
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
                                var a when int.TryParse(a, out var itemInt) => "int",
                                var b when DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var itemDate) => "DateTime",
                                var c when bool.TryParse(c, out var iBool) => "bool",
                                _ => "string"
                            },
                            node.InnerText switch
                            {
                                var a when int.TryParse(a, out var itemInt) => "0",
                                var b when DateTime.TryParse(b, CultureInfo.InvariantCulture, DateTimeStyles.None, out var itemDate) => "DateTime.MinValue",
                                var c when bool.TryParse(c, out var iBool) => "false",
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

        /// <summary>
        /// Initial settings for the Monaco Editor.
        /// </summary>
        /// <param name="editor"></param>
        /// <returns><see cref="StandaloneEditorConstructionOptions"/></returns>
        private StandaloneEditorConstructionOptions EditorConstructionOptions(StandaloneCodeEditor editor)
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

        /// <summary>
        /// Changes the color theme of the Monaco Editor
        /// </summary>
        /// <param name="theme"></param>
        private async Task ChangeTheme(string theme)
        {
            _monacoTheme = theme;
            try
            {
                var myTheme = theme;
                if (!new[] { "vs-dark", "vs-light" }.Contains(theme))
                {
                    await Global.DefineTheme(JS, "thisTheme",
                        await ApiClient.GetFromJsonAsync<StandaloneThemeData>($"themes/{theme}.json"));
                    myTheme = "thisTheme";
                }

                await Global.SetTheme(JS, myTheme);
                await JS.InvokeVoidAsync("localStorage.setItem", "MonacoTheme", theme);
            }
            catch (Exception ex)
            {
                await DialogService.Alert(ex.StackTrace, ex.Message);
            }
        }

        /// <summary>
        /// Executes user input transform
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        private async Task JavaScript()
        {
            try
            {
                if (string.IsNullOrEmpty(_input))
                    throw new ArgumentException("Input is Empty");

                var userCode = await Editor.GetValue();

                if (string.IsNullOrEmpty(userCode))
                    throw new ArgumentException("Please enter or choose a function.");
                if (!userCode.Contains("output"))
                    throw new ArgumentException("Please assign a value to output");
                if (!userCode.Contains("input"))
                    throw new ArgumentException("You must use the input.");

                await JS.InvokeVoidAsync("runUserScript", userCode);
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

        /// <summary>
        /// Updates the user code from drop down selection
        /// </summary>
        /// <param name="jsTransform"></param>
        private Task UpdateUserCode(string jsTransform)
        {
            var fullString = _jsTransforms.Where(x => x.Code == jsTransform)
                .Select(y => $"{y.Name}\n{y.Code}").FirstOrDefault();
            return Editor.SetValue(fullString);
        }

        /// <summary>
        /// Sets the next code in JsTransforms
        /// </summary>
        private async Task NextJs()
        {
            try
            {
                var userCode = await Editor.GetValue();
                if (string.IsNullOrEmpty(userCode))
                    userCode = $"{_jsTransforms[0].Name}\n{_jsTransforms[0].Code}";
                else
                {
                    var index = _jsTransforms.FindIndex(x => x.Name == userCode.Split("\n")[0]);
                    userCode = index < _jsTransforms.Count - 1
                        ? $"{_jsTransforms[index + 1].Name}\n{_jsTransforms[index + 1].Code}"
                        : $"{_jsTransforms[0].Name}\n{_jsTransforms[0].Code}";
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

        /// <summary>
        /// Sets the last code in JsTransforms.
        /// </summary>
        private async Task PreviousJs()
        {
            try
            {
                var userCode = await Editor.GetValue();
                if (string.IsNullOrEmpty(userCode))
                    userCode = $"{_jsTransforms[0].Name}\n{_jsTransforms[0].Code}";
                else
                {
                    var index = _jsTransforms.FindIndex(x => x.Name == userCode.Split("\n")[0]);
                    if (index > 0)
                    {
                        userCode = $"{_jsTransforms[index - 1].Name}\n{_jsTransforms[index - 1].Code}";
                    }
                    else
                        userCode = $"{_jsTransforms[^1].Name}\n{_jsTransforms[^1].Code}";
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

        /// <summary>
        /// Deletes selected JsTransform from local storage (browser) and from the JsTransforms currently in memory.
        /// </summary>
        /// <remarks>
        /// I have a hashed password in this method, but it doesn't really do anything since there is no ability
        /// to permanently delete from the transforms in the json file that is initially loaded. This is just a proof of concept.
        /// </remarks>
        private async Task DeleteJs()
        {
            try
            {
                var userCode = await Editor.GetValue();
                var name = userCode.Split("\n")[0];
                var jsTransform = _jsTransforms.Find(x => x.Name == name);
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

                    using var hash = SHA256.Create();
                    if ("+aGrr99mKAuTRZp/t0aSzvD6vSHtr0nNv4NFTVuxTH0="
                        !.Equals(Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(_entry)))))
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

        /// <summary>
        /// Saves a new user code or updates an existing one.
        /// </summary>
        /// <remarks>
        /// Saves new transforms to local storage and updates the JsTransforms list in memory.
        /// Since it is in local storage, it will carry between sessions, but will be deleted if the cache is cleared.
        /// </remarks>
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

                if (_jsTransforms.Exists(x => x.Name == name))
                    _jsTransforms.Remove(_jsTransforms.Find(x => x.Name == name)!);

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

        private Task<dynamic> OutputModalView() =>
            DialogService.OpenAsync<CustomDialog>(
                "Output",
                new Dictionary<string, object>
                {
                    { "Message", _output ?? "" }
                },
                new DialogOptions()
                {
                    Resizable = true,
                    Draggable = true,
                    CloseDialogOnOverlayClick = true,
                    Width = "80vw",
                    Height = "80vh"
                });

        #endregion
    }
}
