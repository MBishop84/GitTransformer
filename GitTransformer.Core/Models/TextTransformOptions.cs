namespace GitTransformer.Core.Models;

public sealed record TextBounds(string Prefix = "", string Suffix = "");

public sealed record TextTransformOptions(
    string? Split,
    string? Join,
    TextBounds BoundAll,
    TextBounds BoundEach,
    bool TreatNumbersAsUnbounded,
    bool Sort,
    bool RemoveDuplicates);
