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
        await GitService.CreateShadowDocBranchAsync();

        // tijdelijk
        var toolRoot = FindToolRoot();

        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(f => toolRoot == null || !f.StartsWith(toolRoot, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in changedFiles)
        {
            Console.WriteLine($"Changed file: {file}");
        }

        foreach (var file in changedFiles)
        {
            var fileContents = await CodeAnalysisService.AnalyzeAsync(new List<string> { file });            
            var content = fileContents.First().Content;

            var types = new List<DocumentationType> { DocumentationType.ClassDescriptionAndMethodDescription };
            if (content.Contains("[HttpGet]") || content.Contains("[HttpPost]"))
                types.Add(DocumentationType.ApiFlow);
            if (content.Contains(": I") || content.Contains("interface"))
                types.Add(DocumentationType.Relationship);

            foreach (var type in types)
            {
                var doc = await AiDocumentationService.GenerateDocumentationAsync(fileContents, type);
                await MarkdownWriterService.WriteAsync(doc, type);
            }
        }

        await GitService.CommitAndPushAsync("docs: auto-generated documentation for changed files: " + string.Join(", ", changedFiles.Select(Path.GetFileName)));

    }

    //alleen tijdelijk

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
