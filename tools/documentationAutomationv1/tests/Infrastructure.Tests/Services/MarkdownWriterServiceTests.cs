using src.Application.DTOs;
using src.Infrastructure;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class MarkdownWriterServiceTests : IDisposable
{
    private readonly string _tempBasePath;
    private readonly MarkdownWriterService _sut;

    public MarkdownWriterServiceTests()
    {
        _tempBasePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        _sut = new MarkdownWriterService(_tempBasePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempBasePath))
            Directory.Delete(_tempBasePath, recursive: true);
    }

    [Fact]
    public async Task WriteAsync_ClassMethodType_CreatesFileInClassMethodDocumentationFolder()
    {
        await _sut.WriteAsync("content", DocumentationType.ClassDescriptionAndMethodDescription);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "ClassMethodDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_ApiFlowType_CreatesFileInApiFlowDocumentationFolder()
    {
        await _sut.WriteAsync("content", DocumentationType.ApiFlow);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "ApiFlowDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_RelationshipType_CreatesFileInRelationshipDocumentationFolder()
    {
        await _sut.WriteAsync("content", DocumentationType.Relationship);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "RelationshipDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_WritesCorrectContentToFile()
    {
        var expectedContent = "# My Documentation\nSome content here.";

        await _sut.WriteAsync(expectedContent, DocumentationType.ApiFlow);

        var filePath = Directory.GetFiles(Path.Combine(_tempBasePath, "ApiFlowDocumentation")).Single();
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Equal(expectedContent, actualContent);
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        Assert.False(Directory.Exists(Path.Combine(_tempBasePath, "ClassMethodDocumentation")));

        await _sut.WriteAsync("content", DocumentationType.ClassDescriptionAndMethodDescription);

        Assert.True(Directory.Exists(Path.Combine(_tempBasePath, "ClassMethodDocumentation")));
    }

    [Fact]
    public async Task WriteAsync_FileName_HasCorrectFormat()
    {
        await _sut.WriteAsync("content", DocumentationType.Relationship);

        var filePath = Directory.GetFiles(Path.Combine(_tempBasePath, "RelationshipDocumentation")).Single();
        var fileName = Path.GetFileName(filePath);
        Assert.Matches(@"^documentation_\d{8}_\d{6}\.md$", fileName);
    }

    [Fact]
    public async Task WriteAsync_UnknownDocumentationType_ThrowsArgumentOutOfRangeException()
    {
        var unknownType = (DocumentationType)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.WriteAsync("content", unknownType));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task WriteAsync_EmptyOrWhitespaceContent_ThrowsArgumentException(string? content)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _sut.WriteAsync(content!, DocumentationType.ApiFlow));
    }

    [Fact]
    public async Task WriteAsync_OutputDirectoryIsFile_ThrowsIOException()
    {
        // Maak een bestand aan op de plek waar de submap aangemaakt moet worden
        Directory.CreateDirectory(_tempBasePath);
        var conflictPath = Path.Combine(_tempBasePath, "ApiFlowDocumentation");
        await File.WriteAllTextAsync(conflictPath, "blocking file");

        await Assert.ThrowsAsync<IOException>(() =>
            _sut.WriteAsync("content", DocumentationType.ApiFlow));
    }
}
