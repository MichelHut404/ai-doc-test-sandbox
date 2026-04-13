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
        var changedFiles = (await GitService.GetChangedFilesAsync())
            .Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
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

        
        
    }


}
