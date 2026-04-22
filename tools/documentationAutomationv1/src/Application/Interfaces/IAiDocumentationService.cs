
using src.Application.DTOs;

namespace documentationAutomationv1.Application.Interfaces;

public interface IAiDocumentationService
{
    Task<string> GenerateDocumentationAsync(IEnumerable<FileContent> fileContents, DocumentationType documentationType, string language);
}
