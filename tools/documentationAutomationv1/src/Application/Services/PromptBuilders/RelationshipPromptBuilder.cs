using documentationAutomationv1.Application.DTOs;
using documentationAutomationv1.Application.Interfaces;
using src.Domain.ValueObjects;

namespace src.Application.Services.PromptBuilders;

public class RelationshipPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType
    {
        get { return DocumentationType.Relationship; }
    }

    public Type OutputType => typeof(RelationshipDocumentation);
    public string Build(string filesSections) => $"""
        Generate documentation in English for the following C# files, focusing on class and interface relationships.
        Describe inheritance, implementations, and associations.
        Make sure to write it in markdown format.
        Use only plain markdown: headings, bullet points, bold, code blocks, and tables.
        Plain text only — no HTML tags and no underline syntax.

        {filesSections}
        """;
}