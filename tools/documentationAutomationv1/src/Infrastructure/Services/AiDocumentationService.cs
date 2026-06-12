namespace src.Infrastructure.Services;

using documentationAutomationv1.Application.Interfaces;
using src.Domain.ValueObjects;
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

    public async Task<IDocumentationOutput> GenerateDocumentationAsync(IEnumerable<FileContent> fileContents, DocumentationType documentationType, string language)
    {
        // Zet de bestandsinhoud om naar tekst, met de bestandsnaam als koptekst.
        // Ondersteunt meerdere bestanden, maar wordt normaal aangeroepen met één bestand per keer.
        var filesSections = string.Join("\n\n", fileContents.Select(f =>
            $"=== {f.FileName} ===\n{f.Content}"));

        // Zoek de juiste prompt builder op basis van het gevraagde documentatietype.
        // Als er geen builder bekend is voor dit type, gooi dan een fout.
        if (!_promptBuilders.TryGetValue(documentationType, out var builder) || builder is null)
            throw new ArgumentOutOfRangeException(nameof(documentationType), documentationType, "No prompt builder registered for the specified documentation type.");

        // Bouw de prompt op met de bestandsinhoud erin verwerkt.
        var prompt = builder.Build(filesSections);

        // Stuur de prompt naar de AI en geef het gestructureerde resultaat terug.
        // Het systeem-bericht vertelt de AI in welke programmeertaal de code is geschreven.
        return await _chatClient.GenerateStructuredResponseAsync(
            $"You are a technical documentation assistant for {language} code.",
            prompt,
            builder.OutputType);
    }
}