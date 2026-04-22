using System.Text.Json;
using documentationAutomationv1.Application.Interfaces;
using Microsoft.Extensions.FileSystemGlobbing;
using src.Application.DTOs;

namespace documentationAutomationv1.Application.Orchestrators;

public class CliOrchestrator : BaseOrchestrator
{
    public CliOrchestrator(
        IAiDocumentationService aiDocumentationService,
        ICodeAnalysisService codeAnalysisService,
        IGitService gitService,
        IMarkdownWriterService markdownWriterService)
        : base(aiDocumentationService, codeAnalysisService, gitService, markdownWriterService)
    {
    }

    public override async Task RunAsync()
    {
        Console.WriteLine("[docs] Starting documentation generation...");

        Console.WriteLine("[docs] Loading settings from docsettings.json...");
        var settings = LoadSettings();
        Console.WriteLine($"[docs] Language extension: .{settings.languageFileExtension}");
        Console.WriteLine($"[docs] Exclude patterns: {(settings.Exclude.Count == 0 ? "(none)" : string.Join(", ", settings.Exclude))}");

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
            .Where(f => !settings.Exclude.Any(pattern => IsExcluded(f, gitRoot, pattern)))
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
    // voeg hier nieuwe DocumentationTypes toe als dat nodig is.
    private static IEnumerable<DocumentationType> DetermineDocumentationTypes(string content)
    {
        yield return DocumentationType.ClassDescriptionAndMethodDescription;

        if (content.Contains("[HttpGet]") || content.Contains("[HttpPost]"))
            yield return DocumentationType.ApiFlow;

        if (content.Contains(": I") || content.Contains("interface"))
            yield return DocumentationType.Relationship;
    }


    private static string? FindToolRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName + Path.DirectorySeparatorChar;
            dir = dir.Parent;
        }
        return null;
    }

    private static DocSettings LoadSettings()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var settingsFile = Path.Combine(dir.FullName, "docsettings.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                return JsonSerializer.Deserialize<DocSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new DocSettings();
            }

            if (dir.GetDirectories(".git").Length > 0)
                break;

            dir = dir.Parent;
        }
        return new DocSettings();
    }

    private static bool IsExcluded(string absolutePath, string repoRoot, string pattern)
    {
        var normalizedRoot = repoRoot.TrimEnd(Path.DirectorySeparatorChar, '/') + Path.DirectorySeparatorChar;
        var relativePath = absolutePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? absolutePath[normalizedRoot.Length..].Replace('\\', '/')
            : absolutePath.Replace('\\', '/');

        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        return matcher.Match(relativePath).HasMatches;
    }
}
