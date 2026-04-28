using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

namespace documentationAutomationv1.Application.Orchestrators;

public abstract class BaseOrchestrator
{
    protected IAiDocumentationService AiDocumentationService;
    protected ICodeAnalysisService CodeAnalysisService;
    protected IGitService GitService;
    protected IMarkdownWriterService MarkdownWriterService;
    

    protected BaseOrchestrator(
        IAiDocumentationService aiDocumentationService, ICodeAnalysisService codeAnalysisService, IGitService gitService, IMarkdownWriterService markdownWriterService)
    {
        AiDocumentationService = aiDocumentationService;
        CodeAnalysisService = codeAnalysisService;
        GitService = gitService;
        MarkdownWriterService = markdownWriterService;
    }

    public abstract Task RunAsync();

    protected static IEnumerable<DocumentationType> DetermineDocumentationTypes(string content)
    {
        yield return DocumentationType.ClassDescriptionAndMethodDescription;

        if (content.Contains("[HttpGet]") || content.Contains("[HttpPost]"))
            yield return DocumentationType.ApiFlow;

        if (content.Contains(": I") || content.Contains("interface"))
            yield return DocumentationType.Relationship;
    }

    protected static string? FindToolRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            //TODO: look at .slnx
            if (dir.GetFiles("*.sln").Length > 0)
                return dir.FullName + Path.DirectorySeparatorChar;
            dir = dir.Parent;
        }
        return null;
    }
}
