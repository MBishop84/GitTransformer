using Newtonsoft.Json.Linq;
using PluralizeService.Core;
using System.Text.RegularExpressions;

namespace GitTransformer;

public static class Extensions
{
    public static string? DecoratorFormat { get; set; }

    public static string Singular(this string inputString)
        => PluralizationProvider.IsSingular(inputString)
            ? inputString
            : PluralizationProvider.Singularize(inputString);

    public static string Plural(this string inputString)
        => PluralizationProvider.IsPlural(inputString)
            ? inputString
            : PluralizationProvider.Pluralize(inputString);

    public static string PascalCase(this string input)
            => string.Join("",
                new Regex(@"\W+").Replace(input, "_").Split("_")
                .Select(x => x.Length > 0 ? $"{char.ToUpper(x[0])}{x[1..]}" : ""));

    public static string GetJPropertyType(this JProperty token)
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
                _ => $"IEnumerable<{GetJPropertyType(new JProperty(token.Name.PascalCase().Singular(), (token.Value as JArray)![0]))}>",
            },
            JTokenType.Object => token.Name.PascalCase(),
            _ => "string"
        };

    public static List<string> ProcessJArray(this JProperty property)
    {
        List<string> records = [];
        var recordName = property.Name.Singular();
        List<string> fields = [];
        var first = JArray.Parse(property.Value.ToString()).First;

        if (first == null)
            return records;

        if (first.Type == JTokenType.Object)
        {
            records.AddRange(new JProperty(recordName, first).ProcessJProperty());
        }
        else if (first.Type == JTokenType.Array)
        {
            records.AddRange(new JProperty(recordName, first).ProcessJArray());
        }
        else
        {
            fields.Add($"""
                {property.Name.GetDecorator()}IEnumerable<{new JProperty(recordName, first).GetJPropertyType()}>? {property.Name.PascalCase().Plural()} = null
            """);
        }

        if (fields.Count > 0)
            records.Add($"public record {recordName.PascalCase()}(\n{string.Join(",\n", fields)});");

        return records;
    }

    public static List<string> ProcessJProperty(this JProperty property)
    {
        List<string> records = [];
        var recordName = property.Name;
        List<string> fields = [];

        if (property.Value.Type == JTokenType.Object)
        {
            foreach (var child in property.Value.Children<JProperty>())
            {
                fields.Add($"""
                    {child.Name.GetDecorator()}{child.GetJPropertyType()}? {child.Name.PascalCase()} = null
                """);
                if (child.Value.Type == JTokenType.Object)
                    records.AddRange(child.ProcessJProperty());
                else if (child.Value.Type == JTokenType.Array)
                    records.AddRange(child.ProcessJArray());
            }
        }
        else if (property.Value.Type == JTokenType.Array)
        {
            records.AddRange(property.ProcessJArray());
        }
        else
        {
            fields.Add($"""
                {property.Name.GetDecorator()}{property.GetJPropertyType()}? {property.Name.PascalCase()} = null
            """);
        }

        if (fields.Count > 0)
            records.Add($"public record {recordName.PascalCase()}(\n{string.Join(",\n", fields)});");

        return records;
    }

    public static string GetDecorator(this string name) =>
        string.IsNullOrEmpty(DecoratorFormat) ? "" : $"{string.Format(DecoratorFormat, name)} ";
}
