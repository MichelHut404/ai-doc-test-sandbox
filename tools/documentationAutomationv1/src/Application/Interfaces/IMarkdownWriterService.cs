using src.Domain.ValueObjects;

namespace documentationAutomationv1.Application.Interfaces;

public interface IMarkdownWriterService
{
    Task WriteAsync(IDocumentationOutput content, DocumentationType documentationType);
}
