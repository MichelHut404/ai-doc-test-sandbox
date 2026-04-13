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

        // Check if HEAD~1 exists (fails on shallow clones or first commit)
        var parentCheck = await _processRunner.RunAsync("git", "rev-parse --verify HEAD~1");
        var diffArgs = string.IsNullOrWhiteSpace(parentCheck)
            ? "diff --name-only --cached HEAD"  // fallback: staged files on first commit
            : "diff --name-only HEAD~1 HEAD";

        var output = await _processRunner.RunAsync("git", diffArgs);
        
        Console.WriteLine($"Git output:\n{output}"); 

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(f => Path.Combine(repoRoot, f.Trim().Replace('/', Path.DirectorySeparatorChar)));
    }

}