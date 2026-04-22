using documentationAutomationv1.Application.Interfaces;
using Moq;
using src.Application.DTOs;
using src.Infrastructure.Interfaces;
using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class AiDocumentationServiceTests
{
    private readonly Mock<IChatClient> _chatClientMock;
    private readonly Mock<IPromptBuilder> _promptBuilderMock;
    private readonly AiDocumentationService _sut;

    public AiDocumentationServiceTests()
    {
        _chatClientMock = new Mock<IChatClient>();
        _promptBuilderMock = new Mock<IPromptBuilder>();
        _promptBuilderMock.Setup(p => p.DocumentationType).Returns(DocumentationType.ApiFlow);

        _sut = new AiDocumentationService(_chatClientMock.Object, [_promptBuilderMock.Object]);
    }

    // Verifieert dat de prompt builder wordt aangeroepen met een samengevoegde sectie van alle bestanden.
    // De service combineert de bestandsnaam en -inhoud in het formaat '=== FileName.cs ===\ncontent'.
    // 'Build' op de prompt builder wordt exact één keer aangeroepen met deze gecombineerde string.
    [Fact]
    public async Task GenerateDocumentationAsync_CallsPromptBuilderWithFilesSections()
    {
        var files = new[] { new FileContent("File.cs", "public class Foo {}") };
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("generated docs");

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "csharp");

        _promptBuilderMock.Verify(p => p.Build(It.Is<string>(s => s.Contains("File.cs") && s.Contains("public class Foo {}"))), Times.Once);
    }

    // Verifieert dat de chat client wordt aangeroepen met de output van de prompt builder.
    // De systeemprompt bevat de meegegeven taal (bv. 'csharp') via een string.
    // De gebruikersprompt is de waarde die 'IPromptBuilder.Build' retourneert.
    [Fact]
    public async Task GenerateDocumentationAsync_CallsChatClientWithPromptBuilderOutput()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        _promptBuilderMock.Setup(p => p.Build(It.IsAny<string>())).Returns("built prompt");
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), "built prompt"))
            .ReturnsAsync("generated docs");

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "csharp");

        _chatClientMock.Verify(c => c.GenerateResponseAsync(
            "You are a technical documentation assistant for csharp code.",
            "built prompt"), Times.Once);
    }

    // Verifieert dat de service de ruwe response van de chat client ongewijzigd teruggeeft.
    // De service voert geen transformatie uit op de gegenereerde tekst;
    // wat 'GenerateResponseAsync' retourneert, is direct het resultaat van 'GenerateDocumentationAsync'.
    [Fact]
    public async Task GenerateDocumentationAsync_ReturnsChatClientResponse()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("expected output");

        var result = await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "csharp");

        Assert.Equal("expected output", result);
    }

    // Verifieert dat een onbekend 'DocumentationType' een 'ArgumentOutOfRangeException' gooit.
    // De service zoekt via '_promptBuilders.TryGetValue' naar een geregistreerde builder.
    // Als er geen builder gevonden wordt voor het opgegeven type, wordt de uitzondering gegooid.
    [Fact]
    public async Task GenerateDocumentationAsync_UnknownDocumentationType_ThrowsArgumentOutOfRangeException()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        var unknownType = (DocumentationType)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.GenerateDocumentationAsync(files, unknownType, "csharp"));
    }

    // Verifieert dat meerdere bestanden worden samengevoegd tot één string voor de prompt builder.
    // De service gebruikt 'string.Join' met een dubbele newline als scheidingsteken.
    // Zo bevat de uiteindelijke sectie de bestandsnamen en inhoud van alle meegegeven bestanden.
    [Fact]
    public async Task GenerateDocumentationAsync_MultipleFiles_CombinesAllInFilesSections()
    {
        var files = new[]
        {
            new FileContent("A.cs", "class A {}"),
            new FileContent("B.cs", "class B {}")
        };
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("docs");

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow, "csharp");

        _promptBuilderMock.Verify(p => p.Build(
            It.Is<string>(s => s.Contains("A.cs") && s.Contains("B.cs"))), Times.Once);
    }
}
