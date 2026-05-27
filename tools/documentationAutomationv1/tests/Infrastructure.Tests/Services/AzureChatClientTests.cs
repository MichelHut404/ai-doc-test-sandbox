using System.ClientModel.Primitives;
using System.Net;
using System.Net.Http.Headers;
using Azure.AI.OpenAI;
using documentationAutomationv1.Application.DTOs;
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

    // ── GenerateStructuredResponseAsync ──────────────────────────────────────

    // Verifieert dat GenerateStructuredResponseAsync een exception gooit wanneer het endpoint niet bereikbaar is.
    [Fact]
    public async Task GenerateStructuredResponseAsync_WithUnreachableEndpoint_ThrowsException()
    {
        var client = new AzureChatClient(ValidApiKey, "https://localhost:1/", ValidDeploymentName);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            client.GenerateStructuredResponseAsync("system", "user", typeof(ClassMethodDocumentation)));
    }

    // Verifieert dat GenerateStructuredResponseAsync het juiste IDocumentationOutput-object retourneert
    // wanneer de API een geldig JSON-antwoord teruggeeft.
    // De TransformSchemaNode-lambda wordt gedekt via het 'object'-knooppunt (true-branch)
    // én via string-knooppunten (false-branch) in het schema van ClassMethodDocumentation.
    [Fact]
    public async Task GenerateStructuredResponseAsync_WithValidResponse_ReturnsDeserializedOutput()
    {
        // content is a JSON string value — inner quotes must be JSON-escaped so the outer
        // response body is valid JSON. Raw string literals preserve backslashes literally,
        // so `\"` here becomes the two characters \ and " which JSON parsers treat as an
        // escaped double-quote inside a string value.
        const string apiResponseJson = """
            {
              "id": "chatcmpl-test",
              "object": "chat.completion",
              "created": 1718591579,
              "model": "gpt-4o",
              "choices": [{
                "index": 0,
                "message": { "role": "assistant", "content": "{\"FileName\":\"Test.cs\",\"FileDescription\":\"A test file\",\"Classes\":[]}" },
                "finish_reason": "stop"
              }],
              "usage": { "prompt_tokens": 10, "completion_tokens": 20, "total_tokens": 30 }
            }
            """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(apiResponseJson)
        };
        httpResponse.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        var options = new AzureOpenAIClientOptions(AzureOpenAIClientOptions.ServiceVersion.V2024_10_21);
        options.Transport = new HttpClientPipelineTransport(new HttpClient(new MockHttpMessageHandler(httpResponse)));

        var client = new AzureChatClient(ValidApiKey, ValidEndpoint, ValidDeploymentName, options);

        var result = await client.GenerateStructuredResponseAsync("system", "user", typeof(ClassMethodDocumentation));

        var doc = Assert.IsType<ClassMethodDocumentation>(result);
        Assert.Equal("Test.cs", doc.FileName);
        Assert.Equal("A test file", doc.FileDescription);
        Assert.Empty(doc.Classes);
    }
}

internal sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        => Task.FromResult(response);
}