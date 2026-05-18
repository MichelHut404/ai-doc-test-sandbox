using documentationAutomationv1.Application.Interfaces;

namespace src.Infrastructure.Interfaces;

public interface IChatClient
{
    Task<string> GenerateResponseAsync(string systemMessage, string userMessage);
    Task<IDocumentationOutput> GenerateStructuredResponseAsync(string systemMessage, string userMessage, Type outputType);
}