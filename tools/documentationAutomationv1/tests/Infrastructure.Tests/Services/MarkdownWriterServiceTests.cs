using documentationAutomationv1.Application.DTOs;
using src.Domain.ValueObjects;
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
        var content = new ClassMethodDocumentation("test.cs", "desc", new List<ClassDoc>());
        await _sut.WriteAsync(content, DocumentationType.ClassDescriptionAndMethodDescription);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "ClassMethodDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_ApiFlowType_CreatesFileInApiFlowDocumentationFolder()
    {
        var content = new ApiFlowDocumentation("summary", new List<EndpointDoc>());
        await _sut.WriteAsync(content, DocumentationType.ApiFlow);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "ApiFlowDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_RelationshipType_CreatesFileInRelationshipDocumentationFolder()
    {
        var content = new RelationshipDocumentation("summary", new List<RelationshipDoc>());
        await _sut.WriteAsync(content, DocumentationType.Relationship);

        var files = Directory.GetFiles(Path.Combine(_tempBasePath, "RelationshipDocumentation"));
        Assert.Single(files);
    }

    [Fact]
    public async Task WriteAsync_WritesCorrectContentToFile()
    {
        var content = new ApiFlowDocumentation("My API summary", new List<EndpointDoc>());

        await _sut.WriteAsync(content, DocumentationType.ApiFlow);

        var filePath = Directory.GetFiles(Path.Combine(_tempBasePath, "ApiFlowDocumentation")).Single();
        var actualContent = await File.ReadAllTextAsync(filePath);
        Assert.Contains("My API summary", actualContent);
    }

    [Fact]
    public async Task WriteAsync_CreatesDirectoryIfNotExists()
    {
        var content = new ClassMethodDocumentation("test.cs", "desc", new List<ClassDoc>());
        Assert.False(Directory.Exists(Path.Combine(_tempBasePath, "ClassMethodDocumentation")));

        await _sut.WriteAsync(content, DocumentationType.ClassDescriptionAndMethodDescription);

        Assert.True(Directory.Exists(Path.Combine(_tempBasePath, "ClassMethodDocumentation")));
    }

    [Fact]
    public async Task WriteAsync_FileName_HasCorrectFormat()
    {
        var content = new RelationshipDocumentation("summary", new List<RelationshipDoc>());
        await _sut.WriteAsync(content, DocumentationType.Relationship);

        var filePath = Directory.GetFiles(Path.Combine(_tempBasePath, "RelationshipDocumentation")).Single();
        var fileName = Path.GetFileName(filePath);
        Assert.Matches(@"^documentation_\d{8}_\d{6}\.md$", fileName);
    }

    [Fact]
    public async Task WriteAsync_UnknownDocumentationType_ThrowsArgumentOutOfRangeException()
    {
        var content = new ApiFlowDocumentation("summary", new List<EndpointDoc>());
        var unknownType = (DocumentationType)999;

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _sut.WriteAsync(content, unknownType));
    }

    [Fact]
    public async Task WriteAsync_NullContent_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _sut.WriteAsync(null!, DocumentationType.ApiFlow));
    }

    [Fact]
    public async Task WriteAsync_OutputDirectoryIsFile_ThrowsIOException()
    {
        // Maak een bestand aan op de plek waar de submap aangemaakt moet worden
        Directory.CreateDirectory(_tempBasePath);
        var conflictPath = Path.Combine(_tempBasePath, "ApiFlowDocumentation");
        await File.WriteAllTextAsync(conflictPath, "blocking file");
        var content = new ApiFlowDocumentation("summary", new List<EndpointDoc>());

        await Assert.ThrowsAsync<IOException>(() =>
            _sut.WriteAsync(content, DocumentationType.ApiFlow));
    }

    // ── ConvertClassMethod ────────────────────────────────────────────────────

    // Verifieert dat de bestandsnaam en beschrijving worden geschreven als header.
    [Fact]
    public async Task WriteAsync_ClassMethod_WritesFileNameAndDescriptionHeader()
    {
        var content = new ClassMethodDocumentation("MyFile.cs", "This is my file.", new List<ClassDoc>());

        await _sut.WriteAsync(content, DocumentationType.ClassDescriptionAndMethodDescription);

        var fileContent = await ReadSingleFileAsync("ClassMethodDocumentation");
        Assert.Contains("# MyFile.cs", fileContent);
        Assert.Contains("This is my file.", fileContent);
    }

    // Verifieert dat de klasse-sectie wordt geschreven met naam en beschrijving.
    [Fact]
    public async Task WriteAsync_ClassMethod_WithClass_WritesClassSection()
    {
        var cls = new ClassDoc("MyClass", "A test class.", new List<MethodDoc>());
        var content = new ClassMethodDocumentation("MyFile.cs", "desc", new List<ClassDoc> { cls });

        await _sut.WriteAsync(content, DocumentationType.ClassDescriptionAndMethodDescription);

        var fileContent = await ReadSingleFileAsync("ClassMethodDocumentation");
        Assert.Contains("## MyClass", fileContent);
        Assert.Contains("A test class.", fileContent);
    }

    // Verifieert dat een methode met parameters en return-type volledig wordt geschreven.
    [Fact]
    public async Task WriteAsync_ClassMethod_WithMethod_WritesMethodSectionWithParametersAndReturn()
    {
        var param = new ParameterDoc("input", "string", "The input value.");
        var method = new MethodDoc("void DoWork(string input)", "Does work.", new List<ParameterDoc> { param }, "void");
        var cls = new ClassDoc("MyClass", "desc", new List<MethodDoc> { method });
        var content = new ClassMethodDocumentation("MyFile.cs", "desc", new List<ClassDoc> { cls });

        await _sut.WriteAsync(content, DocumentationType.ClassDescriptionAndMethodDescription);

        var fileContent = await ReadSingleFileAsync("ClassMethodDocumentation");
        Assert.Contains("### `void DoWork(string input)`", fileContent);
        Assert.Contains("Does work.", fileContent);
        Assert.Contains("**input** (`string`): The input value.", fileContent);
        Assert.Contains("**Returns**: void", fileContent);
    }

    // ── ConvertApiFlow ────────────────────────────────────────────────────────

    // Verifieert dat het API-flow-header en de samenvatting worden geschreven.
    [Fact]
    public async Task WriteAsync_ApiFlow_WritesHeaderWithSummary()
    {
        var content = new ApiFlowDocumentation("Overall API summary.", new List<EndpointDoc>());

        await _sut.WriteAsync(content, DocumentationType.ApiFlow);

        var fileContent = await ReadSingleFileAsync("ApiFlowDocumentation");
        Assert.Contains("# API Flow", fileContent);
        Assert.Contains("Overall API summary.", fileContent);
    }

    // Verifieert dat een endpoint wordt geschreven met methode, route, beschrijving, input en output.
    [Fact]
    public async Task WriteAsync_ApiFlow_WithEndpoint_WritesEndpointDetails()
    {
        var endpoint = new EndpointDoc("GET", "/api/items", "Get all items.", "none", "List<Item>");
        var content = new ApiFlowDocumentation("summary", new List<EndpointDoc> { endpoint });

        await _sut.WriteAsync(content, DocumentationType.ApiFlow);

        var fileContent = await ReadSingleFileAsync("ApiFlowDocumentation");
        Assert.Contains("## `GET /api/items`", fileContent);
        Assert.Contains("Get all items.", fileContent);
        Assert.Contains("**Input**: none", fileContent);
        Assert.Contains("**Output**: List<Item>", fileContent);
    }

    // ── ConvertRelationship ───────────────────────────────────────────────────

    // Verifieert dat het relationship-header en de samenvatting worden geschreven.
    [Fact]
    public async Task WriteAsync_Relationship_WritesHeaderWithSummary()
    {
        var content = new RelationshipDocumentation("Overall relationship summary.", new List<RelationshipDoc>());

        await _sut.WriteAsync(content, DocumentationType.Relationship);

        var fileContent = await ReadSingleFileAsync("RelationshipDocumentation");
        Assert.Contains("# Relationships", fileContent);
        Assert.Contains("Overall relationship summary.", fileContent);
    }

    // Verifieert dat een relatie volledig wordt geschreven, inclusief overerving, implementaties en gebruik.
    // Implementaties en gebruikte klassen worden met ", " samengevoegd.
    [Fact]
    public async Task WriteAsync_Relationship_WithRelationship_WritesInheritsImplementsAndUses()
    {
        var rel = new RelationshipDoc("MyClass", "BaseClass", new List<string> { "IFoo", "IBar" }, new List<string> { "ServiceA", "ServiceB" });
        var content = new RelationshipDocumentation("summary", new List<RelationshipDoc> { rel });

        await _sut.WriteAsync(content, DocumentationType.Relationship);

        var fileContent = await ReadSingleFileAsync("RelationshipDocumentation");
        Assert.Contains("## MyClass", fileContent);
        Assert.Contains("**Inherits**: BaseClass", fileContent);
        Assert.Contains("**Implements**: IFoo, IBar", fileContent);
        Assert.Contains("**Uses**: ServiceA, ServiceB", fileContent);
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private async Task<string> ReadSingleFileAsync(string subFolder)
    {
        var filePath = Directory.GetFiles(Path.Combine(_tempBasePath, subFolder)).Single();
        return await File.ReadAllTextAsync(filePath);
    }
}
