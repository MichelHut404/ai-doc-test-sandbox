using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

namespace src.Infrastructure.Services.PromptBuilders;

public class ApiFlowPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType => DocumentationType.ApiFlow;

    public string Build(string filesSections) => $"""
        Generate API flow documentation in English for the following C# files.
        Describe the API endpoints, their inputs, outputs, and interactions.
        Make sure to write it in markdown format, with code snippets where relevant.

        {filesSections}
        """;
}
