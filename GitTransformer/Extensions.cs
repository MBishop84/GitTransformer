using PluralizeService.Core;
using System.Text.RegularExpressions;

namespace GitTransformer;

public static class Extensions
{
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
}
