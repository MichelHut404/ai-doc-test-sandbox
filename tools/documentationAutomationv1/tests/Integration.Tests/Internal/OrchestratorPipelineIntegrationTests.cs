using documentationAutomationv1.Application.DTOs;
using Microsoft.Extensions.Logging;
using src.Application.Services.PromptBuilders;
using src.Domain.ValueObjects;
using src.Infrastructure.Interfaces;
using src.Infrastructure.Services;

namespace documentationAutomationv1.Integration.Tests.Internal;

/// Integratie tests voor de volledige documentatie pipeline:
/// AzureOrchestrator → AiDocumentationService → PromptBuilders → mock IChatClient.
/// Git, MarkdownWriter en CodeAnalysis worden gemockt zodat alleen de interne koppeling getest wordt.
public class OrchestratorPipelineIntegrationTests
{
    private readonly Mock<IChatClient> _chatClientMock = new();
    private readonly Mock<IGitService> _gitMock = new();
    private readonly Mock<IMarkdownWriterService> _markdownMock = new();
    private readonly Mock<ICodeAnalysisService> _codeAnalysisMock = new();
    private readonly Mock<ISettingsService> _settingsMock = new();

    private static DocSettings DefaultSettings(string ext = "cs") => new()
    {
        languageFileExtension = ext,
        Exclude = []
    };

    private AzureOrchestrator CreateSut()
    {
        var promptBuilders = new IPromptBuilder[]
        {
            new ClassMethodPromptBuilder(),
            new ApiFlowPromptBuilder(),
            new RelationshipPromptBuilder()
        };

        var aiService = new AiDocumentationService(_chatClientMock.Object, promptBuilders);
        var logger = new Mock<ILogger<AzureOrchestrator>>().Object;

        return new AzureOrchestrator(
            aiService,
            _codeAnalysisMock.Object,
            _gitMock.Object,
            _markdownMock.Object,
            _settingsMock.Object,
            logger);
    }

    // Een gewoon .cs bestand zonder [HttpGet] of interface-relaties wordt aangeboden.
    // Verwacht: de orchestrator bepaalt dat alleen ClassDescriptionAndMethodDescription van toepassing is
    // en roept de AI aan met typeof(ClassMethodDocumentation).
    [Fact]
    public async Task RunAsync_WithPlainCsFile_CallsClassMethodDocumentationType()
    {
        var tempFile = CreateTempFile("MyService.cs", "public class MyService {}");

        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings());
        _gitMock.Setup(g => g.GetCurrentBranchAsync()).ReturnsAsync("main");
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync([tempFile]);
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([new FileContent("MyService.cs", "public class MyService {}")]);
        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), typeof(ClassMethodDocumentation)))
            .ReturnsAsync(new ClassMethodDocumentation("MyService.cs", "A service.", []));

        await CreateSut().RunAsync();

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            typeof(ClassMethodDocumentation)),
            Times.Once);
    }

    // Een bestand met [HttpGet] wordt aangeboden.
    // Verwacht: de orchestrator herkent zowel ClassMethod als ApiFlow als documentatietypes
    // en roept de AI twee keer aan met de bijbehorende output types.
    [Fact]
    public async Task RunAsync_WithControllerFile_CallsBothClassMethodAndApiFlowTypes()
    {
        var tempFile = CreateTempFile("OrdersController.cs", "[HttpGet] public IActionResult Get() {}");

        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings());
        _gitMock.Setup(g => g.GetCurrentBranchAsync()).ReturnsAsync("main");
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync([tempFile]);
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([new FileContent("OrdersController.cs", "[HttpGet] public IActionResult Get() {}")]);
        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), typeof(ClassMethodDocumentation)))
            .ReturnsAsync(new ClassMethodDocumentation("OrdersController.cs", "desc", []));
        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), typeof(ApiFlowDocumentation)))
            .ReturnsAsync(new ApiFlowDocumentation("Summary", []));

        await CreateSut().RunAsync();

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), typeof(ClassMethodDocumentation)), Times.Once);
        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), typeof(ApiFlowDocumentation)), Times.Once);
    }

    // De AI retourneert een ClassMethodDocumentation object.
    // Verwacht: datzelfde object wordt ongewijzigd doorgegeven aan de MarkdownWriterService.
    [Fact]
    public async Task RunAsync_DocumentationOutput_IsPassedToMarkdownWriter()
    {
        var tempFile = CreateTempFile("MyService.cs", "public class MyService {}");
        var expectedDoc = new ClassMethodDocumentation("MyService.cs", "A service.", []);

        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings());
        _gitMock.Setup(g => g.GetCurrentBranchAsync()).ReturnsAsync("main");
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync([tempFile]);
        _codeAnalysisMock.Setup(c => c.AnalyzeAsync(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync([new FileContent("MyService.cs", "public class MyService {}")]);
        _chatClientMock
            .Setup(c => c.GenerateStructuredResponseAsync(It.IsAny<string>(), It.IsAny<string>(), typeof(ClassMethodDocumentation)))
            .ReturnsAsync(expectedDoc);

        await CreateSut().RunAsync();

        _markdownMock.Verify(m => m.WriteAsync(expectedDoc, DocumentationType.ClassDescriptionAndMethodDescription), Times.Once);
    }

    // Git retourneert een lege lijst met gewijzigde bestanden.
    // Verwacht: de AI en MarkdownWriter worden nooit aangeroepen omdat er niets te documenteren is.
    [Fact]
    public async Task RunAsync_WhenNoChangedFiles_NeitherAiNorMarkdownIsCalled()
    {
        _settingsMock.Setup(s => s.LoadSettings()).Returns(DefaultSettings());
        _gitMock.Setup(g => g.GetCurrentBranchAsync()).ReturnsAsync("main");
        _gitMock.Setup(g => g.CreateShadowDocBranchAsync()).ReturnsAsync("docs/main");
        _gitMock.Setup(g => g.GetRepoRootAsync()).ReturnsAsync("C:/repo/");
        _gitMock.Setup(g => g.GetChangedFilesAsync()).ReturnsAsync([]);

        await CreateSut().RunAsync();

        _chatClientMock.Verify(c => c.GenerateStructuredResponseAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Type>()), Times.Never);
        _markdownMock.Verify(m => m.WriteAsync(
            It.IsAny<IDocumentationOutput>(), It.IsAny<DocumentationType>()), Times.Never);
    }

    private static string CreateTempFile(string fileName, string content)
    {
        var path = Path.Combine(Path.GetTempPath(), fileName);
        File.WriteAllText(path, content);
        return path;
    }
}