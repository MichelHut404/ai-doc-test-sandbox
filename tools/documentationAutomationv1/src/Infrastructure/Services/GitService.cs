using documentationAutomationv1.Application.Interfaces;
using src.Infrastructure.Interfaces;

namespace src.Infrastructure;

public class GitService : IGitService
{
    private readonly ICMDProcessRunner _processRunner;

    public GitService(ICMDProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<IEnumerable<string>> GetChangedFilesAsync()
    {
        var repoRoot = (await _processRunner.RunAsync("git", "rev-parse --show-toplevel")).Trim();
        var output = await _processRunner.RunAsync("git", "diff --name-only HEAD~1 HEAD");
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => Path.Combine(repoRoot, f.Trim().Replace('/', Path.DirectorySeparatorChar)));
    }

}