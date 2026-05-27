using System.Diagnostics;
using src.Infrastructure;

namespace documentationAutomationv1.Integration.Tests.Gateway;

public class GitServiceIntegrationTests : IDisposable
{
    private readonly string _repoPath;
    private readonly GitService _gitService;

    public GitServiceIntegrationTests()
    {
        _repoPath = Path.Combine(Path.GetTempPath(), $"git-test-{Path.GetRandomFileName()}");
        Directory.CreateDirectory(_repoPath);

        var runner = new DirectoryProcessRunner(_repoPath);
        _gitService = new GitService(runner);

        RunGit("init");
        RunGit("config user.email \"test@test.com\"");
        RunGit("config user.name \"Test User\"");
        RunGit("checkout -b main");

        File.WriteAllText(Path.Combine(_repoPath, "README.md"), "# Test repo");
        RunGit("add .");
        RunGit("commit -m \"Initial commit\"");
    }

    /// Test: roept <c>git rev-parse --show-toplevel</c> aan in een echte temp git repo.
    /// Verwacht: het teruggegeven pad bestaat als directory op schijf.
    [Fact]
    public async Task GetRepoRootAsync_ReturnsExistingDirectory()
    {
        var root = await _gitService.GetRepoRootAsync();

        Assert.True(Directory.Exists(root));
    }

    /// Test: roept <c>git rev-parse --abbrev-ref HEAD</c> aan na het initialiseren van de repo op branch <c>main</c>.
    /// Verwacht: de huidige branch naam is <c>main</c>.
    [Fact]
    public async Task GetCurrentBranchAsync_ReturnsCorrectBranch()
    {
        var branch = await _gitService.GetCurrentBranchAsync();

        Assert.Equal("main", branch);
    }

    /// Test: voegt een tweede commit toe met <c>Service.cs</c> en roept daarna <c>GetChangedFilesAsync</c> aan.
    /// Verwacht: <c>Service.cs</c> staat in de lijst van gewijzigde bestanden (diff HEAD~1..HEAD).
    [Fact]
    public async Task GetChangedFilesAsync_ReturnsChangedFiles_WhenSecondCommitAdded()
    {
        File.WriteAllText(Path.Combine(_repoPath, "Service.cs"), "public class Service {}");
        RunGit("add .");
        RunGit("commit -m \"Add service\"");

        var files = (await _gitService.GetChangedFilesAsync()).ToList();

        Assert.Contains(files, f => f.EndsWith("Service.cs", StringComparison.OrdinalIgnoreCase));
    }

    /// Test: voegt een tweede commit toe met <c>NewFile.cs</c> en roept daarna <c>GetChangedFilesAsync</c> aan.
    /// Verwacht: <c>README.md</c> staat niet in de lijst omdat dat bestand niet is gewijzigd in de laatste commit.
    [Fact]
    public async Task GetChangedFilesAsync_DoesNotReturnUnchangedFiles()
    {
        File.WriteAllText(Path.Combine(_repoPath, "NewFile.cs"), "public class NewFile {}");
        RunGit("add .");
        RunGit("commit -m \"Add new file\"");

        var files = (await _gitService.GetChangedFilesAsync()).ToList();

        Assert.DoesNotContain(files, f => f.EndsWith("README.md", StringComparison.OrdinalIgnoreCase));
    }

    private void RunGit(string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = args,
                WorkingDirectory = _repoPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.WaitForExit();
    }

    public void Dispose()
    {
        try { Directory.Delete(_repoPath, recursive: true); }
        catch { /* best effort */ }
    }
}
