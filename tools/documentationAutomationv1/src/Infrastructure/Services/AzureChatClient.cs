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

    public AzureChatClient(string apiKey, string endpoint, string deploymentName)
    {
        var options = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_10_21);
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
        var schemaNode = JsonSchemaExporter.GetJsonSchemaAsNode(new JsonSerializerOptions(), outputType);

        var options = new OpenAI.Chat.ChatCompletionOptions
        {
            ResponseFormat = OpenAI.Chat.ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: outputType.Name,
                jsonSchema: BinaryData.FromString(schemaNode.ToJsonString()),
                jsonSchemaIsStrict: true)
        };

        var response = await _chatClient.CompleteChatAsync(
            [new OpenAI.Chat.SystemChatMessage(systemMessage), new OpenAI.Chat.UserChatMessage(userMessage)],
            options);

        var json = response.Value.Content[0].Text;
        return (IDocumentationOutput)JsonSerializer.Deserialize(json, outputType)!;
    }
}
