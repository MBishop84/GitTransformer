using GitTransformer.Application.Abstractions;
using GitTransformer.Core.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Text;
using System.Xml;

namespace GitTransformer.Application.Services;

public sealed class TextTransformationService : ITextTransformationService
{
    public string Transform(string input, TextTransformOptions options)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);

        var split = DecodeSeparator(options.Split);
        var join = DecodeSeparator(options.Join);
        IEnumerable<string> output = string.IsNullOrEmpty(split)
            ? input.ToCharArray().Select(character => ApplyBounds(character.ToString(), options))
            : input.Split(split).Select(value => ApplyBounds(value, options));

        if (options.Sort)
        {
            output = output.OrderBy(value => value);
        }

        if (options.RemoveDuplicates)
        {
            output = output.Distinct();
        }

        return $"{options.BoundAll.Prefix}{string.Join(join, output)}{options.BoundAll.Suffix}";
    }

    public string ClassFromQuery(string input, bool includeComments)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        var result = new StringBuilder();

        foreach (var line in input.Split('\n'))
        {
            var properties = line.Split('\t');
            if (properties.Length < 2)
            {
                throw new ArgumentException("Insufficient arguments.\n\n", nameof(input));
            }

            if (includeComments)
            {
                result.Append($"///<summary>\n/// Gets/Sets the {properties[0]}.\n///</summary>\n");
            }

            var nullable = properties.Length >= 3 && properties[2].Equals("YES", StringComparison.OrdinalIgnoreCase)
                ? "?"
                : string.Empty;
            result.Append($"public {GetClrType(properties[1])}{nullable} {properties[0]} {{ get; set; }}\n\n");
        }

        return result.ToString();
    }

    public string XmlToClass(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        input = input.Replace("&lt;", "<").Replace("&gt;", ">");

        var xml = new XmlDocument();
        xml.LoadXml(input);
        var root = xml.DocumentElement ?? throw new ArgumentException("XML must have a root element", nameof(input));
        var result = new StringBuilder($"public class {root.Name}\n{{\n");

        foreach (XmlNode node in root.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Comment)
            {
                continue;
            }

            if (string.IsNullOrEmpty(node.InnerText) || node.ChildNodes.Count > 1)
            {
                result.AppendFormat(
                    "\n\t///<summary>\n\t/// Gets/Sets the {0}.\n\t///</summary>\n\tpublic {1} {0} {{ get; set; }}\n",
                    node.Name,
                    $"{char.ToUpper(node.Name[0])}{node.Name[1..]}");
                continue;
            }

            var (type, defaultValue) = GetXmlPropertyType(node.InnerText);
            result.AppendFormat(
                "\n\t///<summary>\n\t/// Gets/Sets the {0}.\n\t///</summary>\n\tpublic {1} {0} {{ get; set; }} = {2};\n",
                node.Name,
                type,
                defaultValue);
        }

        return result.Append('}').ToString();
    }

    public string JsonToXml(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        var document = JsonConvert.DeserializeXmlNode(input);
        using var stringWriter = new StringWriter();
        stringWriter.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        using var xmlWriter = new XmlTextWriter(stringWriter) { Formatting = System.Xml.Formatting.Indented };
        document?.WriteContentTo(xmlWriter);
        xmlWriter.Flush();
        return stringWriter.ToString();
    }

    public string XmlToJson(string input)
    {
        ArgumentException.ThrowIfNullOrEmpty(input);
        var document = new XmlDocument();
        document.LoadXml(input);
        return JsonConvert.SerializeObject(document, Newtonsoft.Json.Formatting.Indented);
    }

    private static string ApplyBounds(string value, TextTransformOptions options)
        => options.TreatNumbersAsUnbounded && int.TryParse(value, out _)
            ? value
            : $"{options.BoundEach.Prefix}{value}{options.BoundEach.Suffix}";

    private static string DecodeSeparator(string? separator)
        => separator?.Replace("\\n", "\n").Replace("\\t", "\t") ?? string.Empty;

    private static string GetClrType(string databaseType)
        => databaseType switch
        {
            var type when type.Contains("int", StringComparison.OrdinalIgnoreCase) => "int",
            var type when type.Contains("date", StringComparison.OrdinalIgnoreCase) => "DateTime",
            var type when type.Contains("bit", StringComparison.OrdinalIgnoreCase) => "bool",
            var type when type.Contains("unique", StringComparison.OrdinalIgnoreCase) => "Guid",
            _ => "string"
        };

    private static (string Type, string DefaultValue) GetXmlPropertyType(string value)
        => value switch
        {
            var item when int.TryParse(item, out _) => ("int", "0"),
            var item when DateTime.TryParse(item, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) => ("DateTime", "DateTime.MinValue"),
            var item when bool.TryParse(item, out _) => ("bool", "false"),
            _ => ("string", "string.Empty")
        };
}
