using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

namespace src.Infrastructure.Services.PromptBuilders;

public class ClassMethodPromptBuilder : IPromptBuilder
{
    public DocumentationType DocumentationType => DocumentationType.ClassDescriptionAndMethodDescription;

    public string Build(string filesSections) => $"""
        Generate clear technical documentation in English for the following C# files.
        Describe per file what it does, which classes it contains and what those classes do.
        For each class, describe all methods including their purpose, parameters, and return values.
        Make sure to write it in markdown format, with code snippets where relevant.

        {filesSections}
        """;
}
