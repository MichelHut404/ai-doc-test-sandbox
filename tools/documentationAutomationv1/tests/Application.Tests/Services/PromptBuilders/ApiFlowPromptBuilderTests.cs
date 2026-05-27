using documentationAutomationv1.Application.DTOs;
using src.Application.Services.PromptBuilders;

namespace documentationAutomationv1.Application.Tests.Services.PromptBuilders;

public class ApiFlowPromptBuilderTests
{
    private readonly ApiFlowPromptBuilder _sut = new();

    [Fact]
    public void DocumentationType_ReturnsApiFlow()
    {
        Assert.Equal(DocumentationType.ApiFlow, _sut.DocumentationType);
    }

    [Fact]
    public void OutputType_ReturnsApiFlowDocumentation()
    {
        Assert.Equal(typeof(ApiFlowDocumentation), _sut.OutputType);
    }

    [Fact]
    public void Build_ContainsFilesSections()
    {
        const string filesSections = "// WeatherController.cs\n[ApiController] public class WeatherController {}";

        var result = _sut.Build(filesSections);

        Assert.Contains(filesSections, result);
    }

    [Fact]
    public void Build_ContainsApiFlowInstructions()
    {
        var result = _sut.Build("section");

        Assert.Contains("API", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("endpoints", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("markdown", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Build_WithEmptyFilesSections_ReturnsPromptWithEmptySection()
    {
        var result = _sut.Build(string.Empty);

        Assert.NotEmpty(result);
        Assert.Contains("API", result, StringComparison.OrdinalIgnoreCase);
    }
}