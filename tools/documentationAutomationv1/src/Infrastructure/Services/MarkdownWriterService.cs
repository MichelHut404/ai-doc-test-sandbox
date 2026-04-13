using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

namespace src.Infrastructure;

public class MarkdownWriterService : IMarkdownWriterService
{
    private readonly string _basePath;

    public MarkdownWriterService(string? basePath = null)
    {
        _basePath = basePath ?? "../Application/Documentation";
    }

    public async Task WriteAsync(string content, DocumentationType documentationType)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Content may not be empty.", nameof(content));

        //TODO: Ai documentatie de titel laten genereren.
        var fileName = $"documentation_{DateTime.Now:yyyyMMdd_HHmmss}.md";
        string subFolder = documentationType switch
        {
            DocumentationType.ClassDescriptionAndMethodDescription => "ClassMethodDocumentation",
            DocumentationType.ApiFlow => "ApiFlowDocumentation",
            DocumentationType.Relationship => "RelationshipDocumentation",
            _ => throw new ArgumentOutOfRangeException(nameof(documentationType), documentationType, null)
        };

        var outputPath = Path.Combine(_basePath, subFolder);

        if (!Directory.Exists(outputPath))
            Directory.CreateDirectory(outputPath);

        var filePath = Path.Combine(outputPath, fileName);

        await File.WriteAllTextAsync(filePath, content);
    }

    

}