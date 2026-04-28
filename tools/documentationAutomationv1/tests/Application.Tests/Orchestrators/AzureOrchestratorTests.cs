namespace documentationAutomationv1.Application.Tests.Orchestrators;

public class AzureOrchestratorTests
{
    private readonly Mock<IAiDocumentationService> _aiMock = new();
    private readonly Mock<ICodeAnalysisService> _codeAnalysisMock = new();
    private readonly Mock<IGitService> _gitMock = new();
    private readonly Mock<IMarkdownWriterService> _markdownMock = new();
    private readonly Mock<ISettingsService> _settingsMock = new();

    private AzureOrchestrator CreateSut() => new(
        _aiMock.Object,
        _codeAnalysisMock.Object,
        _gitMock.Object,
        _markdownMock.Object,
        _settingsMock.Object);

    private static DocSettings DefaultSettings(string ext = "cs") => new()
    {
        languageFileExtension = ext,
        Exclude = new List<string>()
    };

    // --- RunAsync: no changed files ---

    [Fact]
    public async Task RunAsync_WhenNoChangedFilesMatchExtension_SkipsDocumentationGeneration()
    {
        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings("cs"));
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { "file.js", "file.txt" });

        await CreateSut().RunAsync();

        _codeAnalysisMock.Verify(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        _aiMock.Verify(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()), Times.Never);
        _gitMock.Verify(g => g.CommitAndPushAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RunAsync_WhenChangedFilesListIsEmpty_SkipsDocumentationGeneration()
    {
        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings());
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(Enumerable.Empty<string>());

        await CreateSut().RunAsync();

        _codeAnalysisMock.Verify(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()), Times.Never);
        _gitMock.Verify(g => g.CommitAndPushAsync(It.IsAny<string>()), Times.Never);
    }

    // --- RunAsync: happy path ---

    [Fact]
    public async Task RunAsync_WhenChangedFilesExist_CallsAnalyzeWithMatchingFiles()
    {
        var settings = DefaultSettings("cs");
        _settingsMock.Setup(s => s.LoadSettings()).Returns(settings);
        _settingsMock.Setup(s => s.IsExcluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/feature");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");

        var csFile = Path.GetTempFileName() + ".cs";
        File.WriteAllText(csFile, "public class Foo {}");

        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { csFile });
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { new FileContent(csFile, "public class Foo {}") });
        _aiMock.Setup(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()))
            .ReturnsAsync("# Doc");
        _gitMock.Setup(g => g.CommitAndPushAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _gitMock.Setup(g => g.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateSut().RunAsync();

        _codeAnalysisMock.Verify(c => c.AnalyzeAsync(It.Is<IEnumerable<string>>(f => f.Contains(csFile))), Times.Once);

        File.Delete(csFile);
    }

    [Fact]
    public async Task RunAsync_WhenChangedFilesExist_CommitsAndCreatesPullRequest()
    {
        var settings = DefaultSettings("cs");
        _settingsMock.Setup(s => s.LoadSettings()).Returns(settings);
        _settingsMock.Setup(s => s.IsExcluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/feature");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");

        var csFile = Path.GetTempFileName() + ".cs";
        File.WriteAllText(csFile, "public class Foo {}");

        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { csFile });
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { new FileContent(csFile, "public class Foo {}") });
        _aiMock.Setup(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()))
            .ReturnsAsync("# Doc");
        _gitMock.Setup(g => g.CommitAndPushAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _gitMock.Setup(g => g.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateSut().RunAsync();

        _gitMock.Verify(g => g.CommitAndPushAsync(It.IsAny<string>()), Times.Once);
        _gitMock.Verify(g => g.CreatePullRequestAsync("docs/feature", "feature", It.IsAny<string>()), Times.Once);

        File.Delete(csFile);
    }

    [Fact]
    public async Task RunAsync_WhenChangedFilesExist_GeneratesDocumentationForEachType()
    {
        var settings = DefaultSettings("cs");
        _settingsMock.Setup(s => s.LoadSettings()).Returns(settings);
        _settingsMock.Setup(s => s.IsExcluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");

        var csFile = Path.GetTempFileName() + ".cs";
        // Content triggers all three DocumentationTypes
        File.WriteAllText(csFile, "public class Foo : IFoo { [HttpGet] void Get(){} }");

        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { csFile });
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { new FileContent(csFile, "public class Foo : IFoo { [HttpGet] void Get(){} }") });
        _aiMock.Setup(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()))
            .ReturnsAsync("# Doc");
        _gitMock.Setup(g => g.CommitAndPushAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _gitMock.Setup(g => g.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateSut().RunAsync();

        // 3 types × 1 file = 3 calls
        _aiMock.Verify(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()), Times.Exactly(3));
        _markdownMock.Verify(m => m.WriteAsync(It.IsAny<string>(), It.IsAny<DocumentationType>()), Times.Exactly(3));

        File.Delete(csFile);
    }

    // --- RunAsync: exclusion filter ---

    [Fact]
    public async Task RunAsync_WhenFileIsExcluded_SkipsFile()
    {
        var settings = new DocSettings { languageFileExtension = "cs", Exclude = new List<string> { "excluded/" } };
        _settingsMock.Setup(s => s.LoadSettings()).Returns(settings);
        _settingsMock.Setup(s => s.IsExcluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(true);
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");

        var csFile = Path.GetTempFileName() + ".cs";
        File.WriteAllText(csFile, "class Foo {}");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { csFile });

        await CreateSut().RunAsync();

        _codeAnalysisMock.Verify(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()), Times.Never);

        File.Delete(csFile);
    }

    // --- PR title: branch name trimming ---

    [Theory]
    [InlineData("docs/feature-x", "feature-x")]
    [InlineData("docs/main", "main")]
    [InlineData("other/branch", "other/branch")]
    public async Task RunAsync_PullRequestTitle_ContainsCorrectTargetBranch(string docBranch, string expectedTarget)
    {
        var settings = DefaultSettings("cs");
        _settingsMock.Setup(s => s.LoadSettings()).Returns(settings);
        _settingsMock.Setup(s => s.IsExcluded(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(false);
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync(docBranch);
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");

        var csFile = Path.GetTempFileName() + ".cs";
        File.WriteAllText(csFile, "class Foo {}");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync(new[] { csFile });
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new[] { new FileContent(csFile, "class Foo {}") });
        _aiMock.Setup(a => a.GenerateDocumentationAsync(It.IsAny<IEnumerable<FileContent>>(), It.IsAny<DocumentationType>(), It.IsAny<string>()))
            .ReturnsAsync("# Doc");
        _gitMock.Setup(g => g.CommitAndPushAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        _gitMock.Setup(g => g.CreatePullRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);

        await CreateSut().RunAsync();

        _gitMock.Verify(g => g.CreatePullRequestAsync(docBranch, expectedTarget, It.Is<string>(t => t.Contains(expectedTarget))), Times.Once);

        File.Delete(csFile);
    }
}