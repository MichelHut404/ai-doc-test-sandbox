
using src.Domain.ValueObjects;

namespace documentationAutomationv1.Application.Interfaces;

public interface IAiDocumentationService
{
    Task<IDocumentationOutput> GenerateDocumentationAsync(IEnumerable<FileContent> fileContents, DocumentationType documentationType, string language);
}
