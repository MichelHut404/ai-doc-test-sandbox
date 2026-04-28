using src.Infrastructure.Interfaces;
using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class AzureChatClientTests
{
    private const string ValidApiKey = "test-api-key";
    private const string ValidEndpoint = "https://test.openai.azure.com/";
    private const string ValidDeploymentName = "gpt-4o";

    // ── Constructor ──────────────────────────────────────────────────────────

    // Verifieert dat de constructor een instantie aanmaakt wanneer geldige parameters worden meegegeven.
    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        var client = new AzureChatClient(ValidApiKey, ValidEndpoint, ValidDeploymentName);

        Assert.NotNull(client);
    }

    // Verifieert dat AzureChatClient de IChatClient-interface implementeert.
    [Fact]
    public void Constructor_WithValidParameters_ImplementsIChatClient()
    {
        var client = new AzureChatClient(ValidApiKey, ValidEndpoint, ValidDeploymentName);

        Assert.IsAssignableFrom<IChatClient>(client);
    }

    // Verifieert dat een ArgumentNullException wordt gegooid wanneer de apiKey null is.
    // ApiKeyCredential vereist een niet-null waarde voor de sleutel.
    [Fact]
    public void Constructor_WithNullApiKey_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AzureChatClient(null!, ValidEndpoint, ValidDeploymentName));
    }

    // Verifieert dat een ArgumentNullException wordt gegooid wanneer het endpoint null is.
    // new Uri(null) gooit een ArgumentNullException.
    [Fact]
    public void Constructor_WithNullEndpoint_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AzureChatClient(ValidApiKey, null!, ValidDeploymentName));
    }

    // Verifieert dat een UriFormatException wordt gegooid wanneer het endpoint een lege string is.
    // new Uri("") is geen geldige URI en gooit een UriFormatException.
    [Fact]
    public void Constructor_WithEmptyEndpoint_ThrowsUriFormatException()
    {
        Assert.Throws<UriFormatException>(() =>
            new AzureChatClient(ValidApiKey, string.Empty, ValidDeploymentName));
    }

    // Verifieert dat een UriFormatException wordt gegooid wanneer het endpoint geen geldige URI is.
    [Fact]
    public void Constructor_WithInvalidEndpointFormat_ThrowsUriFormatException()
    {
        Assert.Throws<UriFormatException>(() =>
            new AzureChatClient(ValidApiKey, "not-a-valid-uri", ValidDeploymentName));
    }

    // ── GenerateResponseAsync ─────────────────────────────────────────────────

    // Verifieert dat GenerateResponseAsync een exception gooit wanneer het endpoint niet bereikbaar is.
    // Omdat de interne ChatClient niet kan worden gemockt, wordt een onbereikbaar endpoint gebruikt
    // om te bevestigen dat netwerkfouten correct worden doorgegeven aan de aanroeper.
    [Fact]
    public async Task GenerateResponseAsync_WithUnreachableEndpoint_ThrowsException()
    {
        var client = new AzureChatClient(ValidApiKey, "https://localhost:1/", ValidDeploymentName);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.GenerateResponseAsync("system prompt", "user message"));
    }
}