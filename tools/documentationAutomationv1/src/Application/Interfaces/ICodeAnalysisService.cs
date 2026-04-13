using src.Application.DTOs;

namespace documentationAutomationv1.Application.Interfaces;

public interface ICodeAnalysisService
{
    Task<IEnumerable<FileContent>> AnalyzeAsync(IEnumerable<string> filePaths);

}
