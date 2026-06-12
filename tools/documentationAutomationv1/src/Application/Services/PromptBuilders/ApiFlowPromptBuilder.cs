using documentationAutomationv1.Application.DTOs;
using documentationAutomationv1.Application.Interfaces;
using src.Domain.ValueObjects;

namespace src.Application.Services.PromptBuilders;

public class ApiFlowPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType
    {
        get { return DocumentationType.ApiFlow; }
    }

    public Type OutputType => typeof(ApiFlowDocumentation);

    public string Build(string filesSections) => $"""
        Generate API flow documentation in English for the following C# files.
        Describe the API endpoints, their inputs, outputs, and interactions.
        Make sure to write it in markdown format.
        Use only plain markdown: headings, bullet points, bold, code blocks, and tables.
        Plain text only — no HTML tags and no underline syntax.

        {filesSections}
        """;
}