using System.ClientModel;
using Azure.AI.OpenAI;
using documentationAutomationv1.Application.Interfaces;

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
        var response = await _chatClient.CompleteChatAsync(
        [
            new OpenAI.Chat.SystemChatMessage(systemMessage),
            new OpenAI.Chat.UserChatMessage(userMessage)
        ]);

        return response.Value.Content[0].Text;
    }
}
