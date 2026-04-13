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
        var filePaths = Directory.GetFiles("samples", "*.cs", SearchOption.AllDirectories);
        Console.WriteLine($"Found {filePaths.Length} file(s) to document.");

        for (int i = 0; i < filePaths.Length; i++)
        {
            var fileContents = await CodeAnalysisService.AnalyzeAsync([filePaths[i]]);
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
