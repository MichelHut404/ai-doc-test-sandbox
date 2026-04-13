using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

namespace src.Infrastructure.Services.PromptBuilders;

public class RelationshipPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType => DocumentationType.Relationship;

    public string Build(string filesSections) => $"""
        Generate documentation in English for the following C# files, focusing on class and interface relationships.
        Describe inheritance, implementations, and associations.
        Make sure to write it in markdown format, with code snippets where relevant.

        {filesSections}
        """;
}
