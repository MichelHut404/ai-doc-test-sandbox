using documentationAutomationv1.Application.Interfaces;
namespace documentationAutomationv1.Application.Orchestrators;

public class AzureOrchestrator : BaseOrchestrator
{
    public AzureOrchestrator(
        IAiDocumentationService aiDocumentationService,
        ICodeAnalysisService codeAnalysisService,
        IGitService gitService,
        IMarkdownWriterService markdownWriterService,
        ISettingsService settingsService)
        : base(aiDocumentationService, codeAnalysisService, gitService, markdownWriterService)
    {
        SettingsService = settingsService;
    }

    private ISettingsService SettingsService { get; }

    public override async Task RunAsync()
    {
        Console.WriteLine("[docs] Starting documentation generation...");

        Console.WriteLine("[docs] Loading settings from docsettings.json...");
        var settings = SettingsService.LoadSettings();
        
        Console.WriteLine($"[docs] Language extension: .{settings.languageFileExtension}");
        Console.WriteLine($"[docs] Exclude: {(settings.Exclude.Count == 0 ? "(none)" : string.Join(", ", settings.Exclude))}");

        Console.WriteLine("[docs] Creating shadow doc branch...");
        var docBranch = await GitService.CreateShadowDocBranchAsync();
        var targetBranch = docBranch.StartsWith("docs/") ? docBranch["docs/".Length..] : docBranch;
        Console.WriteLine($"[docs] Doc branch: {docBranch} → target: {targetBranch}");

        var toolRoot = FindToolRoot();
        var gitRoot = await GitService.GetRepoRootAsync();
        Console.WriteLine($"[docs] Git root: {gitRoot}");

        Console.WriteLine("[docs] Fetching changed files...");
        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith($".{settings.languageFileExtension}", StringComparison.OrdinalIgnoreCase))
            .Where(f => toolRoot == null || !f.StartsWith(toolRoot, StringComparison.OrdinalIgnoreCase))
            .Where(f => !settings.Exclude.Any(pattern => SettingsService.IsExcluded(f, gitRoot, pattern)))
            .Where(f => File.Exists(f))
            .ToList();

        if (changedFiles.Count == 0)
        {
            Console.WriteLine("[docs] No changed files to document. Skipping.");
            return;
        }

        Console.WriteLine($"[docs] {changedFiles.Count} file(s) to document:");
        foreach (var file in changedFiles)
            Console.WriteLine($"[docs]   → {file}");

        Console.WriteLine("[docs] Analyzing file contents...");
        var allFileContents = await CodeAnalysisService.AnalyzeAsync(changedFiles);

        foreach (var fileContent in allFileContents)
        {
            var types = DetermineDocumentationTypes(fileContent.Content).ToList();
            Console.WriteLine($"[docs] Generating documentation for: {fileContent.FileName} ({string.Join(", ", types)})");

            foreach (var type in types)
            {
                Console.WriteLine($"[docs]   Generating {type}...");
                var doc = await AiDocumentationService.GenerateDocumentationAsync(new[] { fileContent }, type, settings.languageFileExtension);
                Console.WriteLine($"[docs]   Writing {type} to disk...");
                await MarkdownWriterService.WriteAsync(doc, type);
            }
        }

        Console.WriteLine("[docs] Committing and pushing documentation...");
        await GitService.CommitAndPushAsync("docs: auto-generated documentation for changed files: " + string.Join(", ", changedFiles.Select(Path.GetFileName)));

        var prTitle = $"docs: auto-generated documentation for {targetBranch}";
        Console.WriteLine($"[docs] Creating pull request: \"{prTitle}\"...");
        await GitService.CreatePullRequestAsync(docBranch, targetBranch, prTitle);

        Console.WriteLine("[docs] Done.");
    }

}
