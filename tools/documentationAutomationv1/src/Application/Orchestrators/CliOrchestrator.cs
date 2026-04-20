using documentationAutomationv1.Application.Interfaces;
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

        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => toolRoot == null || !f.StartsWith(toolRoot, StringComparison.OrdinalIgnoreCase))
            .Where(f => File.Exists(f))
            .ToList();

        foreach (var file in changedFiles)
        {
            Console.WriteLine($"Changed file: {file}");
        }

        var allFileContents = await CodeAnalysisService.AnalyzeAsync(changedFiles);

        foreach (var fileContent in allFileContents)
        {
            foreach (var type in DetermineDocumentationTypes(fileContent.Content))
            {
                var doc = await AiDocumentationService.GenerateDocumentationAsync(new[] { fileContent }, type);
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

    //alleen tijdelijk totdat ik van de tool een package maak

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
}
