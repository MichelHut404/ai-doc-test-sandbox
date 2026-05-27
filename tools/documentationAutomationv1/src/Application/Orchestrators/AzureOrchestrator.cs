using documentationAutomationv1.Application.Interfaces;
using Microsoft.Extensions.Logging;
namespace documentationAutomationv1.Application.Orchestrators;

public class AzureOrchestrator : BaseOrchestrator
{
    private ISettingsService SettingsService { get; }
    public AzureOrchestrator(
        IAiDocumentationService aiDocumentationService,
        ICodeAnalysisService codeAnalysisService,
        IGitService gitService,
        IMarkdownWriterService markdownWriterService,
        ISettingsService settingsService,
        ILogger<AzureOrchestrator> logger)
        : base(aiDocumentationService, codeAnalysisService, gitService, markdownWriterService, logger)
    {
        SettingsService = settingsService;
    }


    public override async Task RunAsync()
    {
        Logger.LogInformation("Starting documentation generation...");

        Logger.LogInformation("Loading settings from docsettings.json...");
        var settings = SettingsService.LoadSettings();
        
        Logger.LogInformation("Language extension: .{LanguageExtension}", settings.languageFileExtension);
        Logger.LogInformation("Exclude: {Exclude}", settings.Exclude.Count == 0 ? "(none)" : string.Join(", ", settings.Exclude));

        Logger.LogInformation("Creating shadow doc branch...");
        var targetBranch = (await GitService.GetCurrentBranchAsync()).Trim();
        var docBranch = await GitService.CreateShadowDocBranchAsync();
        Logger.LogInformation("Doc branch: {DocBranch} → target: {TargetBranch}", docBranch, targetBranch);

        var toolRoot = FindToolRoot();
        var gitRoot = await GitService.GetRepoRootAsync();
        Logger.LogInformation("Git root: {GitRoot}", gitRoot);

        Logger.LogInformation("Fetching changed files...");
        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith($".{settings.languageFileExtension}", StringComparison.OrdinalIgnoreCase))
            .Where(f => toolRoot == null || !f.StartsWith(toolRoot, StringComparison.OrdinalIgnoreCase))
            .Where(f => !settings.Exclude.Any(pattern => SettingsService.IsExcluded(f, gitRoot, pattern)))
            .Where(f => File.Exists(f))
            .ToList();

        if (changedFiles.Count == 0)
        {
            Logger.LogWarning("No changed files to document. Skipping.");
            return;
        }

        Logger.LogInformation("{FileCount} file(s) to document:", changedFiles.Count);
        foreach (var file in changedFiles)
            Logger.LogInformation("  → {File}", file);

        Logger.LogInformation("Analyzing file contents...");
        var allFileContents = await CodeAnalysisService.AnalyzeAsync(changedFiles);

        foreach (var fileContent in allFileContents)
        {
            var types = DetermineDocumentationTypes(fileContent.Content).ToList();
            Logger.LogInformation("Generating documentation for: {FileName} ({Types})", fileContent.FileName, string.Join(", ", types));

            foreach (var type in types)
            {
                Logger.LogInformation("  Generating {Type}...", type);
                var doc = await AiDocumentationService.GenerateDocumentationAsync(new[] { fileContent }, type, settings.languageFileExtension);
                Logger.LogInformation("  Writing {Type} to disk...", type);
                await MarkdownWriterService.WriteAsync(doc, type);
            }
        }

        Logger.LogInformation("Committing and pushing documentation...");
        await GitService.CommitAndPushAsync("docs: auto-generated documentation for changed files: " + string.Join(", ", changedFiles.Select(Path.GetFileName)));

        var prTitle = $"docs: auto-generated documentation for {targetBranch}";
        Logger.LogInformation("Creating pull request: \"{PrTitle}\"...", prTitle);
        await GitService.CreatePullRequestAsync(docBranch, targetBranch, prTitle);

        Logger.LogInformation("Done.");
    }

}
