using documentationAutomationv1.Application.Interfaces;
using src.Infrastructure.Interfaces;

namespace src.Infrastructure;

public class GitService : IGitService
{
    private readonly IProcessRunner _processRunner;

    public GitService(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    public async Task<IEnumerable<string>> GetChangedFilesAsync()
    {
        var repoRoot = (await _processRunner.RunAsync("git", "rev-parse --show-toplevel")).Trim();

        var parentCheck = await _processRunner.RunAsync("git", "rev-parse --verify HEAD~1");
        var diffArgs = string.IsNullOrWhiteSpace(parentCheck)
            ? "diff --name-only --cached HEAD"  
            : "diff --name-only HEAD~1 HEAD";

        var output = await _processRunner.RunAsync("git", diffArgs);
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => Path.Combine(repoRoot, f.Trim().Replace('/', Path.DirectorySeparatorChar)));
    }

    public async Task<string> CreateShadowDocBranchAsync()
    {
        var currentBranch = (await _processRunner.RunAsync("git", "rev-parse --abbrev-ref HEAD")).Trim();
        var shadowBranch = $"docs/{currentBranch}";

        var branchExists = await _processRunner.RunAsync("git", $"rev-parse --verify {shadowBranch}");
        if (string.IsNullOrWhiteSpace(branchExists))
            await _processRunner.RunAsync("git", $"checkout -b {shadowBranch}");
        else
            await _processRunner.RunAsync("git", $"checkout {shadowBranch}");

        return shadowBranch;
    }

}