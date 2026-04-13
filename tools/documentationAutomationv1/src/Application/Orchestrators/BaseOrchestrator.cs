using documentationAutomationv1.Application.Interfaces;

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


}
