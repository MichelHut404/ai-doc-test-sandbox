
namespace documentationAutomationv1.Application.Tests.Services.PromptBuilders;

public class ClassMethodPromptBuilderTests
{
    private readonly ClassMethodPromptBuilder _sut = new();

    [Fact]
    public void DocumentationType_ReturnsClassDescriptionAndMethodDescription()
    {
        Assert.Equal(DocumentationType.ClassDescriptionAndMethodDescription, _sut.DocumentationType);
    }

    [Fact]
    public void OutputType_ReturnsClassMethodDocumentation()
    {
        Assert.Equal(typeof(ClassMethodDocumentation), _sut.OutputType);
    }

    [Fact]
    public void Build_ContainsFilesSections()
    {
        const string filesSections = "// OrderService.cs\npublic class OrderService { public void Place() {} }";

        var result = _sut.Build(filesSections);

        Assert.Contains(filesSections, result);
    }

    [Fact]
    public void Build_ContainsClassMethodInstructions()
    {
        var result = _sut.Build("section");

        Assert.Contains("methods", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("classes", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("markdown", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithEmptyFilesSections_ReturnsPromptWithEmptySection()
    {
        var result = _sut.Build(string.Empty);

        Assert.NotEmpty(result);
        Assert.Contains("documentation", result, StringComparison.OrdinalIgnoreCase);
    }
}