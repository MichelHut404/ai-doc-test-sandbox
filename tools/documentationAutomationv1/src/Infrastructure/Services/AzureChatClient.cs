using System.ClientModel;
using System.Text.Json;
using System.Text.Json.Schema;
using Azure.AI.OpenAI;
using documentationAutomationv1.Application.Interfaces;
using src.Infrastructure.Interfaces;

namespace src.Infrastructure.Services;

public class AzureChatClient : IChatClient
{
    private readonly OpenAI.Chat.ChatClient _chatClient;

    public AzureChatClient(string apiKey, string endpoint, string deploymentName, AzureOpenAIClientOptions? options = null)
    {
        options ??= new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_10_21);
        var azureClient = new AzureOpenAIClient(new Uri(endpoint), new ApiKeyCredential(apiKey), options);
        _chatClient = azureClient.GetChatClient(deploymentName);
    }

    public async Task<string> GenerateResponseAsync(string systemMessage, string userMessage)
    {
        var response = await _chatClient.CompleteChatAsync(new OpenAI.Chat.SystemChatMessage(systemMessage),new OpenAI.Chat.UserChatMessage(userMessage));

        return response.Value.Content[0].Text;
    }

    public async Task<IDocumentationOutput> GenerateStructuredResponseAsync(string systemMessage, string userMessage, Type outputType)
    {
        // Genereer automatisch een JSON schema op basis van het C# type (bijv. ClassMethodDocumentation).
        // Dit schema vertelt de AI precies welke velden hij moet invullen.
        var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(
        JsonSerializerOptions.Default,
        outputType,
        new JsonSchemaExporterOptions
        {
            // zorgt ervoor dat alles word ingevuld. Anders zou de AI alleen de velden invullen die hij "kent" en de rest leeg laten.
            TreatNullObliviousAsNonNullable = true,
            // Voeg "additionalProperties: false" toe aan elk object in het schema,
            // zodat de AI geen extra velden mag toevoegen die niet in het record staan.
            TransformSchemaNode = (ctx, node) =>
            {
                if (node is System.Text.Json.Nodes.JsonObject obj &&
                    obj["type"]?.GetValue<string>() == "object")
                {
                    obj["additionalProperties"] = false;
                }
                return node;
            }
        });

        // Stel de response format in op JSON schema modus.
        // Azure OpenAI garandeert hierdoor dat de respons altijd het opgegeven schema volgt.
        var options = new OpenAI.Chat.ChatCompletionOptions
        {
            ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: outputType.Name,
                jsonSchema: BinaryData.FromString(schemaNode.ToJsonString()),
                jsonSchemaIsStrict: true)
        };

        // Stuur de berichten naar de AI en wacht op de respons.
        var response = await _chatClient.CompleteChatAsync(
            [new OpenAI.Chat.SystemChatMessage(systemMessage), new OpenAI.Chat.UserChatMessage(userMessage)],
            options);

        // Zet de JSON-tekst uit de respons om naar het juiste C# object en geef het terug.
        var json = response.Value.Content[0].Text;
        return (IDocumentationOutput)JsonSerializer.Deserialize(json, outputType)!;
    }
}
