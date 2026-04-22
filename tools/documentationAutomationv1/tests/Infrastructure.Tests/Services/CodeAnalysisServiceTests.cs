using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class CodeAnalysisServiceTests : IDisposable
{
    private readonly CodeAnalysisService _sut;
    private readonly List<string> _tempFiles = [];

    public CodeAnalysisServiceTests()
    {
        _sut = new CodeAnalysisService();
    }

    private string CreateTempFile(string content, string fileName = "Test.cs")
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + "_" + fileName);
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
            File.Delete(file);
    }

    // Verifieert dat de inhoud van het bestand correct wordt teruggegeven.
    // 'File.ReadAllTextAsync' leest de volledige bestandsinhoud als string.
    // De service slaat deze op in 'FileContent.Content'.
    [Fact]
    public async Task AnalyzeAsync_SingleFile_ReturnsCorrectContent()
    {
        var path = CreateTempFile("public class Foo {}");

        var result = (await _sut.AnalyzeAsync([path])).ToList();

        Assert.Single(result);
        Assert.Equal("public class Foo {}", result[0].Content);
    }

    // Verifieert dat alleen de bestandsnaam (zonder pad) wordt gebruikt als 'FileName'.
    // 'Path.GetFileName' haalt de naam inclusief extensie op uit het volledige pad.
    // Zo bevat 'FileContent.FileName' geen mappen-informatie.
    [Fact]
    public async Task AnalyzeAsync_SingleFile_ReturnsCorrectFileName()
    {
        var path = CreateTempFile("content", "MyClass.cs");

        var result = (await _sut.AnalyzeAsync([path])).ToList();

        Assert.Equal(Path.GetFileName(path), result[0].FileName);
    }

    // Verifieert dat meerdere bestanden allemaal worden verwerkt en teruggegeven.
    // De service itereert over alle opgegeven paden en voegt elk toe aan de resultatenlijst.
    [Fact]
    public async Task AnalyzeAsync_MultipleFiles_ReturnsAllFileContents()
    {
        var path1 = CreateTempFile("class A {}", "A.cs");
        var path2 = CreateTempFile("class B {}", "B.cs");
        var path3 = CreateTempFile("class C {}", "C.cs");

        var result = (await _sut.AnalyzeAsync([path1, path2, path3])).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(result, r => r.FileName == Path.GetFileName(path1) && r.Content == "class A {}");
        Assert.Contains(result, r => r.FileName == Path.GetFileName(path2) && r.Content == "class B {}");
        Assert.Contains(result, r => r.FileName == Path.GetFileName(path3) && r.Content == "class C {}");
    }

    // Verifieert dat een lege invoerlijst een lege resultatenlijst oplevert.
    // Wanneer geen bestandspaden worden meegegeven, voert de foreach-loop geen iteraties uit.
    [Fact]
    public async Task AnalyzeAsync_EmptyList_ReturnsEmptyCollection()
    {
        var result = await _sut.AnalyzeAsync([]);

        Assert.Empty(result);
    }

    // Verifieert dat een bestand met lege inhoud correct wordt verwerkt.
    // 'File.ReadAllTextAsync' retourneert een lege string voor een leeg bestand.
    // De service gooit geen uitzondering maar geeft een 'FileContent' terug met lege 'Content'.
    [Fact]
    public async Task AnalyzeAsync_FileWithEmptyContent_ReturnsFileContentWithEmptyString()
    {
        var path = CreateTempFile(string.Empty, "Empty.cs");

        var result = (await _sut.AnalyzeAsync([path])).ToList();

        Assert.Single(result);
        Assert.Equal(string.Empty, result[0].Content);
    }
}