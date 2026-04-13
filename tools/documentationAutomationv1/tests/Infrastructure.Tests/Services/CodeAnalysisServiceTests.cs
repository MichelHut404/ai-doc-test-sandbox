using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class CodeAnalysisServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly CodeAnalysisService _sut;

    public CodeAnalysisServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(_tempDir);
        _sut = new CodeAnalysisService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task AnalyzeAsync_SingleFile_ReturnsFileContentWithCorrectFileName()
    {
        var filePath = Path.Combine(_tempDir, "MyClass.cs");
        await File.WriteAllTextAsync(filePath, "public class MyClass {}");

        var result = await _sut.AnalyzeAsync([filePath]);

        Assert.Single(result);
        Assert.Equal("MyClass.cs", result.First().FileName);
    }

    [Fact]
    public async Task AnalyzeAsync_SingleFile_ReturnsFileContentWithCorrectContent()
    {
        var filePath = Path.Combine(_tempDir, "MyClass.cs");
        var expectedContent = "public class MyClass {}";
        await File.WriteAllTextAsync(filePath, expectedContent);

        var result = await _sut.AnalyzeAsync([filePath]);

        Assert.Equal(expectedContent, result.First().Content);
    }

    [Fact]
    public async Task AnalyzeAsync_MultipleFiles_ReturnsAllFileContents()
    {
        var file1 = Path.Combine(_tempDir, "A.cs");
        var file2 = Path.Combine(_tempDir, "B.cs");
        await File.WriteAllTextAsync(file1, "class A {}");
        await File.WriteAllTextAsync(file2, "class B {}");

        var result = (await _sut.AnalyzeAsync([file1, file2])).ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, f => f.FileName == "A.cs");
        Assert.Contains(result, f => f.FileName == "B.cs");
    }

    [Fact]
    public async Task AnalyzeAsync_EmptyFilePaths_ReturnsEmptyList()
    {
        var result = await _sut.AnalyzeAsync([]);

        Assert.Empty(result);
    }

    [Fact]
    public async Task AnalyzeAsync_NonExistentFile_ThrowsFileNotFoundException()
    {
        var nonExistentPath = Path.Combine(_tempDir, "DoesNotExist.cs");

        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            _sut.AnalyzeAsync([nonExistentPath]));
    }

    [Fact]
    public async Task AnalyzeAsync_UsesFileNameOnly_NotFullPath()
    {
        var filePath = Path.Combine(_tempDir, "SomeService.cs");
        await File.WriteAllTextAsync(filePath, "content");

        var result = await _sut.AnalyzeAsync([filePath]);

        Assert.Equal("SomeService.cs", result.First().FileName);
        Assert.DoesNotContain(_tempDir, result.First().FileName);
    }
}
