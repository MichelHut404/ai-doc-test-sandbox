using documentationAutomationv1.Application.Interfaces;

namespace src.Infrastructure;

public class GitService : IGitService
{
    //TODO: write service that gets changed files from git and their content
    public Task<IEnumerable<string>> GetChangedFilesAsync()
    {
        throw new NotImplementedException();

    }

    public Task<string> GetFileContentAsync()
    {
        throw new NotImplementedException();

    }
}