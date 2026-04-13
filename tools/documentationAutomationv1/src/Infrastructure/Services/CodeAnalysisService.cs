namespace src.Infrastructure.Services;
using documentationAutomationv1.Application.Interfaces;
using src.Application.DTOs;

public class CodeAnalysisService : ICodeAnalysisService
{
    public async Task<IEnumerable<FileContent>> AnalyzeAsync(IEnumerable<string> filePaths)
    {
        var results = new List<FileContent>();

        foreach (var path in filePaths)
        {
            var content = await File.ReadAllTextAsync(path);
            results.Add(new FileContent(Path.GetFileName(path), content));
        }
        
        return results;
    }

}

