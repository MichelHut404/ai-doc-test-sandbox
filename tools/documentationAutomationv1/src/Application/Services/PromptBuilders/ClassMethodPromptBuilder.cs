using documentationAutomationv1.Application.DTOs;
using documentationAutomationv1.Application.Interfaces;
using src.Domain.ValueObjects;

namespace src.Application.Services.PromptBuilders;

public class ClassMethodPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType => DocumentationType.ClassDescriptionAndMethodDescription;

    public Type OutputType => typeof(ClassMethodDocumentation);
    public string Build(string filesSections) => $"""
        Generate clear technical documentation in English for the following C# files.
        Describe per file what it does, which classes it contains and what those classes do.
        For each class, describe all methods including their purpose, parameters, and return values.
        Make sure to write it in markdown format.
        Use only plain markdown: headings, bullet points, bold, code blocks, and tables.
        Plain text only — no HTML tags and no underline syntax.

        {filesSections}
        """;
}