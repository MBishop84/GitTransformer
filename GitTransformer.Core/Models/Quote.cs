namespace GitTransformer.Core.Models;

public sealed record Author(int? Id = 0, string? Name = "Unknown");

public sealed record Quote(int Id, string Text, Author Author)
{
    public static Quote Empty { get; } = new(0, string.Empty, new Author());
}
