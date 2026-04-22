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
        var docBranch = await GitService.CreateShadowDocBranchAsync();
        var targetBranch = docBranch.StartsWith("docs/") ? docBranch["docs/".Length..] : docBranch;

        // tijdelijk
        var toolRoot = FindToolRoot();
        var settings = LoadSettings();


        var gitRoot = await GitService.GetRepoRootAsync();

        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => toolRoot == null || !f.StartsWith(toolRoot, StringComparison.OrdinalIgnoreCase))
            .Where(f => !settings.Exclude.Any(pattern => IsExcluded(f, gitRoot, pattern)))
            .Where(f => File.Exists(f))
            .ToList();

        if (changedFiles.Count == 0)
        {
            Console.WriteLine("No changed .cs files to document. Skipping.");
            return;
        }

        foreach (var file in changedFiles)
        {
            Console.WriteLine($"Changed file: {file}");
        }
        
        var allFileContents = await CodeAnalysisService.AnalyzeAsync(changedFiles);

        foreach (var fileContent in allFileContents)
        {
            foreach (var type in DetermineDocumentationTypes(fileContent.Content))
            {
                var doc = await AiDocumentationService.GenerateDocumentationAsync(new[] { fileContent }, type, settings.Language);
                await MarkdownWriterService.WriteAsync(doc, type);
            }
        }

        await GitService.CommitAndPushAsync("docs: auto-generated documentation for changed files: " + string.Join(", ", changedFiles.Select(Path.GetFileName)));

        var prTitle = $"docs: auto-generated documentation for {targetBranch}";
        await GitService.CreatePullRequestAsync(docBranch, targetBranch, prTitle);

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
