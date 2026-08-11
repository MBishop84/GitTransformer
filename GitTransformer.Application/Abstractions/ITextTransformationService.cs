using GitTransformer.Core.Models;

namespace GitTransformer.Application.Abstractions;

public interface ITextTransformationService
{
    string Transform(string? input, TextTransformOptions options);
    string ClassFromQuery(string? input, bool includeComments);
    string XmlToClass(string? input);
    string JsonToXml(string? input);
    string XmlToJson(string? input);
}
