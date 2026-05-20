namespace src.Infrastructure.Interfaces;

public interface IChatClient
{
    Task<string> GenerateResponseAsync(string systemMessage, string userMessage);

}