using System.Text.Json;
using documentationAutomationv1.Application.DTOs;
using src.Infrastructure.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace documentationAutomationv1.Integration.Tests.Gateway;

public class AzureChatClientIntegrationTests : IDisposable
{
    private readonly WireMockServer _server;  // De nep-HTTP-server die Azure OpenAI nabootst
    private readonly AzureChatClient _client; // De te testen klasse
    private const string DeploymentName = "test-deployment"; // Naam van het Azure OpenAI-deployment dat in de URL wordt gebruikt

    public AzureChatClientIntegrationTests()
    {
        // Start een lokale WireMock-server op een willekeurige vrije poort
        _server = WireMockServer.Start();

        // Maak de client aan met een nep-API-sleutel en de URL van de WireMock-server
        // Zo worden alle HTTP-calls onderschept door WireMock in plaats van echte Azure
        _client = new AzureChatClient("fake-api-key", _server.Urls[0], DeploymentName);
    }

    /// Test: WireMock simuleert een succesvolle Azure OpenAI response (HTTP 200).
    /// Verwacht: de tekst uit het content veld van de response wordt teruggegeven.
    [Fact]
    public async Task GenerateResponseAsync_ReturnsContent_WhenApiSucceeds()
    {
        // Stel in dat WireMock een 200 OK teruggeeft met een geldige chat completion response
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions") // verwacht pad voor Azure OpenAI
                .UsingPost())                                                        // verwacht HTTP POST
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(BuildChatCompletionResponse("Hello from the API!")));     // nep-antwoord met deze tekst als content

        // Roep de client aan met een systeem- en gebruikersbericht
        var result = await _client.GenerateResponseAsync("You are helpful.", "Say hello.");

        // Controleer dat de tekst uit de content van de AI-response correct wordt teruggegeven
        Assert.Equal("Hello from the API!", result);
    }

    /// Test: WireMock simuleert een interne serverfout (HTTP 500).
    /// Verwacht: de Azure SDK gooit een exception omdat de API niet beschikbaar is.
    [Fact]
    public async Task GenerateResponseAsync_ThrowsException_WhenApiReturns500()
    {
        // Stel in dat WireMock een 500 Internal Server Error teruggeeft
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":{"message":"Internal server error","code":"internal_error"}}""")); // foutbody zoals Azure die stuurt

        // Verwacht dat de client een exception gooit in plaats van stil te falen
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _client.GenerateResponseAsync("system", "user"));
    }

    /// Test: WireMock simuleert een unauthorized response (HTTP 401).
    /// Verwacht: de Azure SDK gooit een exception omdat de API-sleutel ongeldig is.
    [Fact]
    public async Task GenerateResponseAsync_ThrowsException_WhenApiReturns401()
    {
        // Stel in dat WireMock een 401 Unauthorized teruggeeft
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(401)
                .WithHeader("Content-Type", "application/json")
                .WithBody("""{"error":{"message":"Unauthorized","code":"unauthorized"}}""")); // foutbody zoals Azure die stuurt bij ongeldige API-sleutel

        // Verwacht dat de client een exception gooit bij een ongeldige API-sleutel
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _client.GenerateResponseAsync("system", "user"));
    }

    /// Test: WireMock simuleert een Azure OpenAI response waarbij de content JSON bevat
    /// die overeenkomt met het schema van ClassMethodDocumentation.
    /// Verwacht: de JSON wordt correct gedeserialiseerd naar het juiste output-type
    /// met de verwachte waarden voor bestandsnaam, klassen en methoden.
    [Fact]
    public async Task GenerateStructuredResponseAsync_ReturnsDeserializedOutput_WhenApiSucceeds()
    {
        // De JSON die de AI terug zou sturen als content — dit is een volledig ClassMethodDocumentation-object
        var structuredJson = """{"FileName":"TestService.cs","FileDescription":"A test service.","Classes":[{"ClassName":"TestService","Description":"Handles tests.","Methods":[{"Signature":"void Execute()","Description":"Runs a test.","Parameters":[],"Returns":"void"}]}]}""";

        // Stel WireMock in om de structuredJson als content terug te geven in een geldig chat completion antwoord
        _server
            .Given(Request.Create()
                .WithPath($"/openai/deployments/{DeploymentName}/chat/completions")
                .UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithHeader("Content-Type", "application/json")
                .WithBody(BuildChatCompletionResponse(structuredJson))); // wrap de JSON in een volledige Azure OpenAI response

        // Roep de gestructureerde methode aan en geef het verwachte outputtype mee
        var result = await _client.GenerateStructuredResponseAsync(
            "You are a documentation expert.",
            "Document this class.",
            typeof(ClassMethodDocumentation)); // de client deserialiseert de content naar dit type

        // Controleer dat het resultaat van het juiste type is
        var doc = Assert.IsType<ClassMethodDocumentation>(result);
        // Controleer de inhoud van het gedeserialiseerde object
        Assert.Equal("TestService.cs", doc.FileName);
        Assert.Single(doc.Classes);                              // precies één klasse verwacht
        Assert.Equal("TestService", doc.Classes[0].ClassName);
        Assert.Single(doc.Classes[0].Methods);                  // precies één methode verwacht
    }

    // Bouwt een volledige Azure OpenAI chat completion JSON-response op basis van een content-string.
    // Dit simuleert de structuur die de echte Azure API teruggeeft.
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

    // Ruim de WireMock-server op na elke test zodat er geen poorten open blijven staan
    public void Dispose() => _server.Dispose();
}