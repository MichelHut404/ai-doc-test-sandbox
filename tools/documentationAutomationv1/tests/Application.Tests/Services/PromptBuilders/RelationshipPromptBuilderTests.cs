
namespace documentationAutomationv1.Application.Tests.Services.PromptBuilders;

public class RelationshipPromptBuilderTests
{
    private readonly RelationshipPromptBuilder _sut = new();

    [Fact]
    public void DocumentationType_ReturnsRelationship()
    {
        Assert.Equal(DocumentationType.Relationship, _sut.DocumentationType);
    }

    [Fact]
    public void OutputType_ReturnsRelationshipDocumentation()
    {
        Assert.Equal(typeof(RelationshipDocumentation), _sut.OutputType);
    }

    [Fact]
    public void Build_ContainsFilesSections()
    {
        const string filesSections = "// MyClass.cs\npublic class MyClass {}";

        var result = _sut.Build(filesSections);

        Assert.Contains(filesSections, result);
    }

    [Fact]
    public void Build_ContainsRelationshipInstructions()
    {
        var result = _sut.Build("section");

        Assert.Contains("inheritance", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("implementations", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("markdown", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithEmptyFilesSections_ReturnsPromptWithEmptySection()
    {
        var result = _sut.Build(string.Empty);

        Assert.NotEmpty(result);
        Assert.Contains("relationships", result, StringComparison.OrdinalIgnoreCase);
    }
}
