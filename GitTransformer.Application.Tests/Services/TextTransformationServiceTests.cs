using GitTransformer.Application.Services;
using GitTransformer.Core.Models;

namespace GitTransformer.Application.Tests.Services;

public sealed class TextTransformationServiceTests
{
    private readonly TextTransformationService _service = new();

    [Fact]
    public void Transform_AppliesOptionsAndPreservesNumericValues()
    {
        var options = new TextTransformOptions(
            ",",
            @"\n",
            new TextBounds("[", "]"),
            new TextBounds("'", "'"),
            TreatNumbersAsUnbounded: true,
            Sort: true,
            RemoveDuplicates: true);

        var result = _service.Transform("beta,2,alpha,beta", options);

        Assert.Equal("['alpha'\n'beta'\n2]", result);
    }

    [Fact]
    public void ClassFromQuery_MapsDatabaseTypesAndNullability()
    {
        var result = _service.ClassFromQuery("Id\tint\tNO\nCreated\tdatetime\tYES", includeComments: false);

        Assert.Equal(
            "public int Id { get; set; }\n\npublic DateTime? Created { get; set; }\n\n",
            result);
    }

    [Fact]
    public void XmlToClass_InfersPrimitivePropertyTypes()
    {
        var result = _service.XmlToClass("<Person><Age>42</Age><Active>true</Active></Person>");

        Assert.Contains("public class Person", result);
        Assert.Contains("public int Age { get; set; } = 0;", result);
        Assert.Contains("public bool Active { get; set; } = false;", result);
    }

    [Fact]
    public void JsonAndXmlConversions_RoundTripRootContent()
    {
        var xml = _service.JsonToXml("{\"Person\":{\"Name\":\"Ada\"}}");
        var json = _service.XmlToJson(xml);

        Assert.Contains("<Person>", xml);
        Assert.Contains("\"Person\"", json);
        Assert.Contains("\"Name\": \"Ada\"", json);
    }
}
