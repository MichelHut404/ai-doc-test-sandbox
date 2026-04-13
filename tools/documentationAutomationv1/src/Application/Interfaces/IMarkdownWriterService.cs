using src.Application.DTOs;

namespace documentationAutomationv1.Application.Interfaces;

public interface IMarkdownWriterService
{

    Task WriteAsync(string content, DocumentationType documentationType);
}
