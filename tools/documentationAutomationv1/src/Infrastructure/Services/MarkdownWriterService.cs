using documentationAutomationv1.Application.DTOs;
using documentationAutomationv1.Application.Interfaces;
using src.Domain.ValueObjects;

namespace src.Infrastructure;

public class MarkdownWriterService : IMarkdownWriterService
{
    private readonly string _basePath;

    public MarkdownWriterService(string? basePath = null)
    {
        _basePath = basePath ?? "Documentation__BasePath";
    }

    public async Task WriteAsync(IDocumentationOutput content, DocumentationType documentationType)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content), "Content may not be null.");
            
        var markdown = content switch
        {
            ClassMethodDocumentation cmd => ConvertClassMethod(cmd),
            ApiFlowDocumentation afd    => ConvertApiFlow(afd),
            RelationshipDocumentation rd => ConvertRelationship(rd),
            _ => throw new ArgumentOutOfRangeException()
        };

        var fileName = $"documentation_{DateTime.Now:yyyyMMdd_HHmmss}_{Environment.UserName}.md";
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

        await File.WriteAllTextAsync(filePath, markdown);
    }

private static string ConvertClassMethod(ClassMethodDocumentation doc)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"# {doc.FileName}");
    sb.AppendLine(doc.FileDescription);

    foreach (var cls in doc.Classes)
    {
        sb.AppendLine($"\n## {cls.ClassName}");
        sb.AppendLine(cls.Description);

        foreach (var method in cls.Methods)
        {
            sb.AppendLine($"\n### `{method.Signature}`");
            sb.AppendLine(method.Description);
            foreach (var param in method.Parameters)
                sb.AppendLine($"- **{param.Name}** (`{param.Type}`): {param.Description}");
            sb.AppendLine($"- **Returns**: {method.Returns}");
        }
    }
    return sb.ToString();
}

private static string ConvertApiFlow(ApiFlowDocumentation doc)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"# API Flow\n{doc.Summary}");

    foreach (var endpoint in doc.Endpoints)
    {
        sb.AppendLine($"\n## `{endpoint.Method} {endpoint.Route}`");
        sb.AppendLine(endpoint.Description);
        sb.AppendLine($"- **Input**: {endpoint.Input}");
        sb.AppendLine($"- **Output**: {endpoint.Output}");
    }
    return sb.ToString();
}

private static string ConvertRelationship(RelationshipDocumentation doc)
{
    var sb = new System.Text.StringBuilder();
    sb.AppendLine($"# Relationships\n{doc.Summary}");

    foreach (var rel in doc.Relationships)
    {
        sb.AppendLine($"\n## {rel.ClassName}");
        sb.AppendLine($"- **Inherits**: {rel.Inherits}");
        sb.AppendLine($"- **Implements**: {string.Join(", ", rel.Implements)}");
        sb.AppendLine($"- **Uses**: {string.Join(", ", rel.Uses)}");
    }
    return sb.ToString();
}
    

}