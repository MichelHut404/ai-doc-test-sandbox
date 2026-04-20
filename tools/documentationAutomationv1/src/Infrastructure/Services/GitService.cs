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

        var localBranchExists = await _processRunner.RunAsync("git", $"branch --list {shadowBranch}");
        var remoteBranchExists = await _processRunner.RunAsync("git", $"ls-remote --heads origin {shadowBranch}");

        if (!string.IsNullOrWhiteSpace(localBranchExists) || !string.IsNullOrWhiteSpace(remoteBranchExists))
            throw new InvalidOperationException(
                $"There is already an existing '{shadowBranch}' branch. " +
                $"Check if that branch is still needed.\n" +
                $"  - Has the branch already been merged? Delete the branch and run the build again.\n" +
                $"  - Has the branch not been merged? Merge it, delete it, and run the build again.");

        await _processRunner.RunAsync("git", $"checkout -b {shadowBranch}");

        return shadowBranch;
    }

    public async Task CommitAndPushAsync(string message)
    {
        await _processRunner.RunAsync("git", "add -A");
        await _processRunner.RunAsync("git", $"commit -m \"{message}\"");

        var branch = (await _processRunner.RunAsync("git", "rev-parse --abbrev-ref HEAD")).Trim();
        await _processRunner.RunAsync("git", $"push --set-upstream origin {branch}");
    }

    public async Task CreatePullRequestAsync(string docBranch, string targetBranch, string title)
    {
        var body = "Auto-generated documentation via tool.";
        await _processRunner.RunAsync(
            "gh",
            $"pr create --base \"{targetBranch}\" --head \"{docBranch}\" --title \"{title}\" --body \"{body}\"");
    }

}