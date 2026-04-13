namespace documentationAutomationv1.Application.Interfaces;

public interface IChatClient
{
    Task<string> GenerateResponseAsync(string systemMessage, string userMessage);
}
