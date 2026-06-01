using documentationAutomationv1.Application.DTOs;
using src.Domain.ValueObjects;
using src.Infrastructure;

namespace documentationAutomationv1.Integration.Tests.Internal;

public class MarkdownWriterServiceIntegrationTests : IDisposable
{
    private readonly string _basePath;
    private readonly MarkdownWriterService _sut;

    public MarkdownWriterServiceIntegrationTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"docs-test-{Path.GetRandomFileName()}");
        _sut = new MarkdownWriterService(_basePath);
    }

    // ClassMethodDocumentation wordt weggeschreven naar de echte file system in een temp map.
    // Verwacht: de submap ClassMethodDocumentation bestaat en bevat precies Ã©Ã©n bestand.
    [Fact]
    public async Task WriteAsync_ClassMethodDocumentation_CreatesFileInCorrectSubfolder()
    {
        var doc = new ClassMethodDocumentation(
            "MyService.cs",
            "A test service.",
            [new ClassDoc("MyService", "Handles logic.", [])]);

        await _sut.WriteAsync(doc, DocumentationType.ClassDescriptionAndMethodDescription, "test");

        var subfolder = Path.Combine(_basePath, "ClassMethodDocumentation");
        Assert.True(Directory.Exists(subfolder));
        Assert.Single(Directory.GetFiles(subfolder));
    }

    // ApiFlowDocumentation wordt weggeschreven naar de echte file system in een temp map.
    // Verwacht: de submap ApiFlowDocumentation bestaat en bevat precies Ã©Ã©n bestand.
    [Fact]
    public async Task WriteAsync_ApiFlowDocumentation_CreatesFileInCorrectSubfolder()
    {
        var doc = new ApiFlowDocumentation(
            "Handles order endpoints.",
            [new EndpointDoc("GET", "/orders", "Returns all orders.", "none", "List<Order>")]);

        await _sut.WriteAsync(doc, DocumentationType.ApiFlow, "test");

        var subfolder = Path.Combine(_basePath, "ApiFlowDocumentation");
        Assert.True(Directory.Exists(subfolder));
        Assert.Single(Directory.GetFiles(subfolder));
    }

    // ClassMethodDocumentation met bestandsnaam "OrderService.cs" wordt weggeschreven.
    // Verwacht: de inhoud van het gegenereerde markdown-bestand bevat de bestandsnaam.
    [Fact]
    public async Task WriteAsync_ClassMethodDocumentation_FileContainsFileName()
    {
        var doc = new ClassMethodDocumentation(
            "OrderService.cs",
            "Manages orders.",
            []);

        await _sut.WriteAsync(doc, DocumentationType.ClassDescriptionAndMethodDescription, "test");

        var file = Directory.GetFiles(Path.Combine(_basePath, "ClassMethodDocumentation")).Single();
        var content = await File.ReadAllTextAsync(file);

        Assert.Contains("OrderService.cs", content);
    }

    // WriteAsync twee keer aangeroepen met een vertraging van 1 seconde (bestandsnaam is op seconden).
    // Verwacht: er worden twee aparte bestanden aangemaakt, niet Ã©Ã©n overschreven.
    [Fact]
    public async Task WriteAsync_MultipleWrites_CreatesMultipleFiles()
    {
        var doc = new ClassMethodDocumentation("File.cs", "desc", []);

        await _sut.WriteAsync(doc, DocumentationType.ClassDescriptionAndMethodDescription, "test");
        await Task.Delay(1100); // timestamp in bestandsnaam is op seconden
        await _sut.WriteAsync(doc, DocumentationType.ClassDescriptionAndMethodDescription, "test");

        var files = Directory.GetFiles(Path.Combine(_basePath, "ClassMethodDocumentation"));
        Assert.Equal(2, files.Length);
    }

    // WriteAsync aangeroepen met null als content.
    // Verwacht: de service gooit een ArgumentNullException voordat er iets naar schijf geschreven wordt.
    [Fact]
    public async Task WriteAsync_ThrowsArgumentNullException_WhenContentIsNull()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.WriteAsync(null!, DocumentationType.ClassDescriptionAndMethodDescription, "test"));
    }

    public void Dispose()
    {
        try { Directory.Delete(_basePath, recursive: true); }
        catch { /* best effort */ }
    }
}
