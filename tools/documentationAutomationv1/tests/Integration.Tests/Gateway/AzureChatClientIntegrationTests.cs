using System.Text.Json;
using documentationAutomationv1.Application.DTOs;
using src.Infrastructure.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace documentationAutomationv1.Integration.Tests.Gateway;

public class AzureChatClientIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly AzureChatClient _client;
    private const string DeploymentName = "test-deployment";

    public AzureChatClientIntegrationTests()
    {
        _server = WireMockServer.Start();
        _client = new AzureChatClient("fake-api-key", _server.Urls[0], DeploymentName);
    }

    /// Test: WireMock simuleert een succesvolle Azure OpenAI response (HTTP 200).
    /// Verwacht: de tekst uit het <c>content</c> veld van de response wordt teruggegeven.
    [Fact]
    public async Task GenerateResponseAsync_ReturnsContent_WhenApiSucceeds()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(BuildChatCompletionResponse("Hello from the API!")));

        var result = await _client.GenerateResponseAsync("You are helpful.", "Say hello.");

        Assert.Equal("Hello from the API!", result);
    }

    /// Test: WireMock simuleert een interne serverfout (HTTP 500).
    /// Verwacht: de Azure SDK gooit een exception omdat de API niet beschikbaar is.
    [Fact]
    public async Task GenerateResponseAsync_ThrowsException_WhenApiReturns500()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":{"message":"Internal server error","code":"internal_error"}}"""));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _client.GenerateResponseAsync("system", "user"));
    }

    /// Test: WireMock simuleert een unauthorized response (HTTP 401).
    /// Verwacht: de Azure SDK gooit een exception omdat de API-sleutel ongeldig is.
    [Fact]
    public async Task GenerateResponseAsync_ThrowsException_WhenApiReturns401()
    {
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":{"message":"Unauthorized","code":"unauthorized"}}"""));

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _client.GenerateResponseAsync("system", "user"));
    }

    /// Test: WireMock simuleert een Azure OpenAI response waarbij de content JSON bevat
    /// die overeenkomt met het schema van <see cref="ClassMethodDocumentation"/>.
    /// Verwacht: de JSON wordt correct gedeserialiseerd naar het juiste output-type
    /// met de verwachte waarden voor bestandsnaam, klassen en methoden.
    [Fact]
    public async Task GenerateStructuredResponseAsync_ReturnsDeserializedOutput_WhenApiSucceeds()
    {
        var structuredJson = """{"FileName":"TestService.cs","FileDescription":"A test service.","Classes":[{"ClassName":"TestService","Description":"Handles tests.","Methods":[{"Signature":"void Execute()","Description":"Runs a test.","Parameters":[],"Returns":"void"}]}]}""";

        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(BuildChatCompletionResponse(structuredJson)));

        var result = await _client.GenerateStructuredResponseAsync(
            "You are a documentation expert.",
            "Document this class.",
            typeof(ClassMethodDocumentation));

        var doc = Assert.IsType<ClassMethodDocumentation>(result);
        Assert.Equal("TestService.cs", doc.FileName);
        Assert.Single(doc.Classes);
        Assert.Equal("TestService", doc.Classes[0].ClassName);
        Assert.Single(doc.Classes[0].Methods);
    }

    private static string BuildChatCompletionResponse(string content)
    {
        var escapedContent = JsonSerializer.Serialize(content);
        return $$"""
            {
                "id": "chatcmpl-test-id",
                "object": "chat.completion",
                "created": 1677652288,
                "model": "gpt-4",
                "choices": [
                    {
                        "index": 0,
                        "message": {
                            "role": "assistant",
                            "content": {{escapedContent}}
                        },
                        "finish_reason": "stop"
                    }
                ],
                "usage": {
                    "prompt_tokens": 10,
                    "completion_tokens": 5,
                    "total_tokens": 15
                }
            }
            """;
    }

    public void Dispose() => _server.Dispose();
}