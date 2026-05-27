using documentationAutomationv1.Application.DTOs;
using src.Application.Services.PromptBuilders;
using src.Domain.ValueObjects;
using src.Infrastructure.Interfaces;
using src.Infrastructure.Services;

namespace documentationAutomationv1.Integration.Tests.Internal;

public class AiDocumentationServiceIntegrationTests
{
    private readonly Mock<IChatClient> _chatClientMock = new();

    private AiDocumentationService CreateSut(params IPromptBuilder[] builders) =>
        new(_chatClientMock.Object, builders);

    // Echte ClassMethodPromptBuilder gebouwd met de bestandsnaam en inhoud.
    // Verwacht: de prompt die naar de AI gestuurd wordt bevat zowel de bestandsnaam als de bestandsinhoud.
    [Fact]
    public async Task GenerateDocumentationAsync_WithClassMethodBuilder_SendsFileContentInPrompt()
    {
        var sut = CreateSut(new ClassMethodPromptBuilder());
        var files = new[] { new FileContent("MyService.cs", "public class MyService {}") };

        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(new ClassMethodDocumentation("MyService.cs", "A service.", []));

        await sut.GenerateDocumentationAsync(files, DocumentationType.ClassDescriptionAndMethodDescription, "cs");

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains("MyService.cs") && p.Contains("public class MyService {}")),
            typeof(ClassMethodDocumentation)),
            Times.Once);
    }

    // ClassMethodPromptBuilder met taal "csharp" meegegeven.
    // Verwacht: het systeembericht richting de AI bevat de opgegeven taal.
    [Fact]
    public async Task GenerateDocumentationAsync_WithClassMethodBuilder_IncludesLanguageInSystemMessage()
    {
        var sut = CreateSut(new ClassMethodPromptBuilder());
        var files = new[] { new FileContent("File.cs", "content") };

        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(new ClassMethodDocumentation("File.cs", "desc", []));

        await sut.GenerateDocumentationAsync(files, DocumentationType.ClassDescriptionAndMethodDescription, "csharp");

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.Is<string>(s => s.Contains("csharp")),
            It.IsAny<string>(),
            It.IsAny<Type>()),
            Times.Once);
    }

    // ApiFlowPromptBuilder gekoppeld aan de service.
    // Verwacht: de AI wordt aangeroepen met typeof(ApiFlowDocumentation) als output type.
    [Fact]
    public async Task GenerateDocumentationAsync_WithApiFlowBuilder_UsesApiFlowOutputType()
    {
        var sut = CreateSut(new ApiFlowPromptBuilder());
        var files = new[] { new FileContent("Controller.cs", "[HttpGet] public IActionResult Get() {}") };

        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), typeof(ApiFlowDocumentation)))
            .ReturnsAsync(new ApiFlowDocumentation("Summary", []));

        await sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "cs");

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            typeof(ApiFlowDocumentation)),
            Times.Once);
    }

    // Twee bestanden meegegeven aan de service met een echte ClassMethodPromptBuilder.
    // Verwacht: de prompt bevat de secties van beide bestanden samengevoegd in één aanroep naar de AI.
    [Fact]
    public async Task GenerateDocumentationAsync_WithMultipleFiles_CombinesAllFilesInPrompt()
    {
        var sut = CreateSut(new ClassMethodPromptBuilder());
        var files = new[]
        {
            new FileContent("ServiceA.cs", "class A {}"),
            new FileContent("ServiceB.cs", "class B {}")
        };

        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>()))
            .ReturnsAsync(new ClassMethodDocumentation("ServiceA.cs", "desc", []));

        await sut.GenerateDocumentationAsync(files, DocumentationType.ClassDescriptionAndMethodDescription, "cs");

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(),
            It.Is<string>(p => p.Contains("ServiceA.cs") && p.Contains("ServiceB.cs")),
            It.IsAny<Type>()),
            Times.Once);
    }

    // Alleen ClassMethodPromptBuilder geregistreerd, maar ApiFlow documentatietype gevraagd.
    // Verwacht: de service gooit een ArgumentOutOfRangeException omdat er geen builder is voor dit type.
    [Fact]
    public async Task GenerateDocumentationAsync_ThrowsArgumentOutOfRangeException_WhenNoBuilderRegistered()
    {
        var sut = CreateSut(new ClassMethodPromptBuilder());
        var files = new[] { new FileContent("File.cs", "content") };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "cs"));
    }
}