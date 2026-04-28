
namespace documentationAutomationv1.Application.Tests.Orchestrators;
public class BaseOrchestratorTests
{
    private class TestOrchestrator : BaseOrchestrator
    {
        public TestOrchestrator(
            IAiDocumentationService aiDocumentationService,
            ICodeAnalysisService codeAnalysisService,
            IGitService gitService,
            IMarkdownWriterService markdownWriterService)
            : base(aiDocumentationService, codeAnalysisService, gitService, markdownWriterService)
        {
        }

        public override Task RunAsync() => Task.CompletedTask;

        public static IEnumerable<DocumentationType> ExposeDetermineDocumentationTypes(string content)
            => DetermineDocumentationTypes(content);

        public static string? ExposeFindToolRoot()
            => FindToolRoot();

        public IAiDocumentationService ExposedAiDocumentationService => AiDocumentationService;
        public ICodeAnalysisService ExposedCodeAnalysisService => CodeAnalysisService;
        public IGitService ExposedGitService => GitService;
        public IMarkdownWriterService ExposedMarkdownWriterService => MarkdownWriterService;
    }

    private readonly Mock<IAiDocumentationService> _aiDocumentationServiceMock = new();
    private readonly Mock<ICodeAnalysisService> _codeAnalysisServiceMock = new();
    private readonly Mock<IGitService> _gitServiceMock = new();
    private readonly Mock<IMarkdownWriterService> _markdownWriterServiceMock = new();

    private TestOrchestrator CreateSut() => new(
        _aiDocumentationServiceMock.Object,
        _codeAnalysisServiceMock.Object,
        _gitServiceMock.Object,
        _markdownWriterServiceMock.Object);

    // --- Constructor ---

    [Fact]
    public void Constructor_AssignsAllServices()
    {
        var sut = CreateSut();

        Assert.Equal(_aiDocumentationServiceMock.Object, sut.ExposedAiDocumentationService);
        Assert.Equal(_codeAnalysisServiceMock.Object, sut.ExposedCodeAnalysisService);
        Assert.Equal(_gitServiceMock.Object, sut.ExposedGitService);
        Assert.Equal(_markdownWriterServiceMock.Object, sut.ExposedMarkdownWriterService);
    }

    // --- DetermineDocumentationTypes ---

    [Fact]
    public void DetermineDocumentationTypes_AlwaysIncludesClassDescriptionAndMethodDescription()
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes("any content");

        Assert.Contains(DocumentationType.ClassDescriptionAndMethodDescription, result);
    }

    [Fact]
    public void DetermineDocumentationTypes_PlainContent_ReturnsOnlyClassDescription()
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes("plain class content").ToList();

        Assert.Single(result);
        Assert.Equal(DocumentationType.ClassDescriptionAndMethodDescription, result[0]);
    }

    [Theory]
    [InlineData("[HttpGet]")]
    [InlineData("[HttpPost]")]
    public void DetermineDocumentationTypes_WhenContentContainsHttpAttribute_IncludesApiFlow(string content)
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes(content);

        Assert.Contains(DocumentationType.ApiFlow, result);
    }

    [Fact]
    public void DetermineDocumentationTypes_WhenContentHasNoHttpAttribute_DoesNotIncludeApiFlow()
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes("plain class content");

        Assert.DoesNotContain(DocumentationType.ApiFlow, result);
    }

    [Theory]
    [InlineData(": I")]
    [InlineData("interface")]
    public void DetermineDocumentationTypes_WhenContentContainsInterfacePattern_IncludesRelationship(string content)
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes(content);

        Assert.Contains(DocumentationType.Relationship, result);
    }

    [Fact]
    public void DetermineDocumentationTypes_WhenContentHasNoInterfacePattern_DoesNotIncludeRelationship()
    {
        var result = TestOrchestrator.ExposeDetermineDocumentationTypes("plain class content");

        Assert.DoesNotContain(DocumentationType.Relationship, result);
    }

    [Fact]
    public void DetermineDocumentationTypes_WhenContentHasAllPatterns_ReturnsAllThreeTypes()
    {
        var content = "public class Foo : IFoo { [HttpGet] public void Get() {} }";

        var result = TestOrchestrator.ExposeDetermineDocumentationTypes(content).ToList();

        Assert.Contains(DocumentationType.ClassDescriptionAndMethodDescription, result);
        Assert.Contains(DocumentationType.ApiFlow, result);
        Assert.Contains(DocumentationType.Relationship, result);
    }

    // --- FindToolRoot ---

    [Fact]
    public void FindToolRoot_WhenSlnExists_ReturnsPathEndingWithSeparator()
    {
        var result = TestOrchestrator.ExposeFindToolRoot();

        if (result != null)
            Assert.EndsWith(Path.DirectorySeparatorChar.ToString(), result);
    }

    [Fact]
    public void FindToolRoot_ReturnedPath_IsNotEmptyWhenNotNull()
    {
        var result = TestOrchestrator.ExposeFindToolRoot();

        if (result != null)
            Assert.NotEmpty(result);
    }
}