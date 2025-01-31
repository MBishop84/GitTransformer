namespace GitTransformer;

public record SingleQuotableResponse(string Content, string Author);
public record Author(int? Id = 0, string? Name = "Unknown");
public record Quote(int Id, string Text, Author Author)
{
    public Quote() : this(0, string.Empty, new Author()) { }
    public Quote(SingleQuotableResponse? response)
        : this(0, response?.Content ?? string.Empty, new Author(0, response?.Author)) { }
}
public record JsTransform(int Id, string AddedBy, string Name, string Code);
