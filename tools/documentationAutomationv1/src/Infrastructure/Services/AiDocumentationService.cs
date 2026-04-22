namespace src.Infrastructure.Services;

using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;
using src.Infrastructure.Interfaces;

public class AiDocumentationService : IAiDocumentationService
{
    private readonly IChatClient _chatClient;
    private readonly Dictionary<DocumentationType, IPromptBuilder> _promptBuilders;

    public AiDocumentationService(IChatClient chatClient, IEnumerable<IPromptBuilder> promptBuilders)
    {
        _chatClient = chatClient;
        _promptBuilders = promptBuilders.ToDictionary(p => p.DocumentationType);
    }

    //TODO: foreach maken. voor elke file een api call? en dan meerdere tegelijk als het nodig is voor bijvoorbeeld api flows, relaties etc

    public async Task<string> GenerateDocumentationAsync(IEnumerable<FileContent> fileContents, DocumentationType documentationType, string language)
    {
        var filesSections = string.Join("\n\n", fileContents.Select(f =>
            $"=== {f.FileName} ===\n{f.Content}"));

        if (!_promptBuilders.TryGetValue(documentationType, out var builder) || builder is null)
            throw new ArgumentOutOfRangeException(nameof(documentationType), documentationType, "No prompt builder registered for the specified documentation type.");
            
        var prompt = builder.Build(filesSections);

        return await _chatClient.GenerateResponseAsync(
            $"You are a technical documentation assistant for {language} code.",
            prompt);
    }
}

