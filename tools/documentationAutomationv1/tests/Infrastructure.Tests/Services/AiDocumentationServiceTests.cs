using documentationAutomationv1.Application.Interfaces;
using Moq;
using src.Application.DTOs;
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

    [Fact]
    public async Task GenerateDocumentationAsync_CallsPromptBuilderWithFilesSections()
    {
        var files = new[] { new FileContent("File.cs", "public class Foo {}") };
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("generated docs");

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow);

        _promptBuilderMock.Verify(p => p.Build(It.Is<string>(s => s.Contains("File.cs") && s.Contains("public class Foo {}"))), Times.Once);
    }

    [Fact]
    public async Task GenerateDocumentationAsync_CallsChatClientWithPromptBuilderOutput()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        _promptBuilderMock.Setup(p => p.Build(It.IsAny<string>())).Returns("built prompt");
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), "built prompt"))
            .ReturnsAsync("generated docs");

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow);

        _chatClientMock.Verify(c => c.GenerateResponseAsync(
            "You are a technical documentation assistant for C# code.",
            "built prompt"), Times.Once);
    }

    [Fact]
    public async Task GenerateDocumentationAsync_ReturnsChatClientResponse()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        _chatClientMock.Setup(c => c.GenerateResponseAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("expected output");

        var result = await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow);

        Assert.Equal("expected output", result);
    }

    [Fact]
    public async Task GenerateDocumentationAsync_UnknownDocumentationType_ThrowsArgumentOutOfRangeException()
    {
        var files = new[] { new FileContent("File.cs", "content") };
        var unknownType = (DocumentationType)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.GenerateDocumentationAsync(files, unknownType));
    }

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

        await _sut.GenerateDocumentationAsync(files, DocumentationType.ApiFlow);

        _promptBuilderMock.Verify(p => p.Build(
            It.Is<string>(s => s.Contains("A.cs") && s.Contains("B.cs"))), Times.Once);
    }
}
