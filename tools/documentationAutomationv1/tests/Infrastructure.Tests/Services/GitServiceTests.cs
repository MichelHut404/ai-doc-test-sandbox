using Moq;
using src.Infrastructure;
using src.Infrastructure.Interfaces;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class GitServiceTests
{
    private readonly Mock<IProcessRunner> _mockRunner;
    private readonly GitService _sut;

    public GitServiceTests()
    {
        _mockRunner = new Mock<IProcessRunner>();
        _sut = new GitService(_mockRunner.Object);
    }

    // ── GetRepoRootAsync ──────────────────────────────────────────────────────

    // Verifieert dat GetRepoRootAsync het resultaat van 'git rev-parse --show-toplevel' teruggegeven.
    [Fact]
    public async Task GetRepoRootAsync_ReturnsRepoRoot()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("/repo\n");

        var result = await _sut.GetRepoRootAsync();

        Assert.Equal("/repo", result);
    }

    // Verifieert dat de repo root wordt getrimd zodat trailing newlines en spaties worden verwijderd.
    [Fact]
    public async Task GetRepoRootAsync_ResultIsTrimmed()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("  /repo  \n");

        var result = await _sut.GetRepoRootAsync();

        Assert.Equal("/repo", result);
    }

    // __________________getchangedfilesasync tests__________________________:

    // Verifieert dat 'git diff --name-only HEAD~1 HEAD' wordt gebruikt wanneer HEAD~1 bestaat.
    // 'diff --name-only' geeft alleen de bestandsnamen terug (geen inhoud).
    // 'HEAD~1 HEAD' vergelijkt de vorige commit (HEAD~1) met de huidige commit (HEAD),
    // waardoor je ziet welke bestanden zijn gewijzigd in de laatste commit.
    [Fact]
    public async Task GetChangedFilesAsync_WhenHead1Exists_UsesDiffHead1Head()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("/repo\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync("src/Foo.cs\n");

        await _sut.GetChangedFilesAsync();

        _mockRunner.Verify(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD"), Times.Once);
    }


    // Verifieert dat 'git diff --name-only --cached HEAD' wordt gebruikt wanneer HEAD~1 niet bestaat (lege string).
    // Dit is het geval bij een initiële commit (geen vorige commit beschikbaar).
    // '--cached' (ook wel '--staged') vergelijkt de staging area met HEAD,
    // zodat je ziet welke bestanden klaarstaan voor de eerste commit.
    [Fact]
    public async Task GetChangedFilesAsync_WhenHead1DoesNotExist_UsesCachedDiff()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("/repo\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only --cached HEAD")).ReturnsAsync("src/Bar.cs\n");

        await _sut.GetChangedFilesAsync();

        _mockRunner.Verify(r => r.RunAsync("git", "diff --name-only --cached HEAD"), Times.Once);
    }


    // Verifieert dat 'git diff --name-only --cached HEAD' ook wordt gebruikt wanneer 'rev-parse --verify HEAD~1'
    // alleen whitespace retourneert (behandeld als afwezig).
    // 'rev-parse --verify HEAD~1' controleert of de vorige commit bestaat;
    // een lege of whitespace-response betekent dat er geen vorige commit is.
    [Fact]
    public async Task GetChangedFilesAsync_WhenHead1CheckIsWhitespace_UsesCachedDiff()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("/repo\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("   ");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only --cached HEAD")).ReturnsAsync("src/Bar.cs\n");

        await _sut.GetChangedFilesAsync();

        _mockRunner.Verify(r => r.RunAsync("git", "diff --name-only --cached HEAD"), Times.Once);
    }


    // Verifieert dat de repo root correct wordt getrimd vóór gebruik in het pad.
    // 'git rev-parse --show-toplevel' geeft het absolute pad naar de root van de repository terug,
    // maar heeft vaak een trailing newline ('\n'). Deze wordt weggetrimd zodat Path.Combine correct werkt.
    [Fact]
    public async Task GetChangedFilesAsync_RepoRootIsTrimmed_PathStartsWithTrimmedRoot()
    {
        var repoRoot = @"C:\repo";
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync(repoRoot + "  \n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync("src/Foo.cs\n");

        var result = await _sut.GetChangedFilesAsync();

        Assert.StartsWith(repoRoot, result.Single());
    }


    // Verifieert dat de geretourneerde paden worden gecombineerd met de repo root via Path.Combine.
    // Git geeft relatieve paden terug (bv. 'src/Foo.cs'), maar de service zet dit om
    // naar een volledig absoluut pad door de repo root als prefix te gebruiken.
    [Fact]
    public async Task GetChangedFilesAsync_ReturnPathCombinedWithRepoRoot()
    {
        var repoRoot = @"C:\repo";
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync(repoRoot + "\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync("src/Foo.cs\n");

        var result = await _sut.GetChangedFilesAsync();

        var expected = Path.Combine(repoRoot, "src" + Path.DirectorySeparatorChar + "Foo.cs");
        Assert.Equal(expected, result.Single());
    }


    // Verifieert dat alle bestanden uit de diff-output worden teruggegeven als aparte items.
    // Git scheidt bestandsnamen in de output met een newline ('\n').
    // De service splitst deze output en retourneert elk bestand als een apart pad.
    [Fact]
    public async Task GetChangedFilesAsync_MultipleFiles_ReturnsAllFiles()
    {
        var repoRoot = @"C:\repo";
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync(repoRoot + "\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD"))
            .ReturnsAsync("src/Foo.cs\nsrc/Bar.cs\nsrc/Baz.cs\n");

        var result = (await _sut.GetChangedFilesAsync()).ToList();

        Assert.Equal(3, result.Count);
        Assert.Contains(Path.Combine(repoRoot, "src" + Path.DirectorySeparatorChar + "Foo.cs"), result);
        Assert.Contains(Path.Combine(repoRoot, "src" + Path.DirectorySeparatorChar + "Bar.cs"), result);
        Assert.Contains(Path.Combine(repoRoot, "src" + Path.DirectorySeparatorChar + "Baz.cs"), result);
    }


    // Verifieert dat een lege diff-output resulteert in een lege collectie.
    // Dit treedt op wanneer er geen gewijzigde bestanden zijn tussen de commits.
    // 'Split' met 'RemoveEmptyEntries' zorgt dat lege regels niet als bestandsnamen worden teruggegeven.
    [Fact]
    public async Task GetChangedFilesAsync_EmptyDiffOutput_ReturnsEmptyCollection()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync("/repo\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync(string.Empty);

        var result = await _sut.GetChangedFilesAsync();

        Assert.Empty(result);
    }


    // Verifieert dat forward slashes in git-paden worden omgezet naar het OS-specifieke pad-scheidingsteken.
    // Git gebruikt altijd forward slashes ('/') in paden, ook op Windows.
    // De service vervangt deze met 'Path.DirectorySeparatorChar' zodat de paden
    // correct werken op het huidige besturingssysteem (bv. '\' op Windows).
    [Fact]
    public async Task GetChangedFilesAsync_ForwardSlashesReplacedWithOsPathSeparator()
    {
        var repoRoot = @"C:\repo";
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync(repoRoot + "\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync("src/nested/Foo.cs\n");

        var result = await _sut.GetChangedFilesAsync();

        Assert.DoesNotContain('/', result.Single());
    }


    // Verifieert dat bestandsnamen met omringende whitespace correct worden getrimd vóór gebruik in het pad.
    // Git-output kan per regel leading/trailing spaties bevatten.
    // De service trimt elke bestandsnaam zodat Path.Combine geen ongeldige paden produceert.
    [Fact]
    public async Task GetChangedFilesAsync_FileNamesAreTrimmed()
    {
        var repoRoot = @"C:\repo";
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --show-toplevel")).ReturnsAsync(repoRoot + "\n");
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify HEAD~1")).ReturnsAsync("abc123");
        _mockRunner.Setup(r => r.RunAsync("git", "diff --name-only HEAD~1 HEAD")).ReturnsAsync("  src/Foo.cs  \n");

        var result = await _sut.GetChangedFilesAsync();

        var expected = Path.Combine(repoRoot, "src" + Path.DirectorySeparatorChar + "Foo.cs");
        Assert.Equal(expected, result.Single());
    }

    // ── CreateShadowDocBranchAsync ──────────────────────────────────────────────

// Verifieert dat de shadow branch de prefix 'docs/' krijgt gevolgd door de huidige branchnaam.
// 'git rev-parse --abbrev-ref HEAD' geeft de verkorte naam van de huidige branch terug (bv. 'feature/login').
// '--abbrev-ref' staat voor 'abbreviated reference' — zonder deze flag krijg je een volledige ref zoals 'refs/heads/feature/login'.
// De service plakt 'docs/' als prefix voor de shadow branch naam.
[Fact]
public async Task CreateShadowDocBranchAsync_ReturnsShadowBranchName()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("feature/login\n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/feature/login")).ReturnsAsync(string.Empty);

    var result = await _sut.CreateShadowDocBranchAsync();

    Assert.Equal("docs/feature/login", result);
}

// Verifieert dat 'git checkout -b' wordt gebruikt wanneer de shadow branch nog niet bestaat.
// 'branch --list docs/main' retourneert een lege string wanneer de branch lokaal niet bestaat.
// 'ls-remote --heads origin docs/main' retourneert een lege string wanneer de branch remote niet bestaat.
// Als beide leeg zijn → 'checkout -b' maakt de nieuwe branch aan.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenBranchDoesNotExist_CreatesNewBranch()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/main")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/main")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/main")).ReturnsAsync(string.Empty);

    await _sut.CreateShadowDocBranchAsync();

    _mockRunner.Verify(r => r.RunAsync("git", "checkout -b docs/main"), Times.Once);
}

// Verifieert dat een InvalidOperationException wordt gegooid wanneer de shadow branch al lokaal bestaat.
// 'branch --list docs/main' retourneert de branchnaam wanneer de branch lokaal aanwezig is.
// De service gooit een uitzondering zodat de gebruiker handmatig de situatie kan oplossen
// (bv. samenvoegen of verwijderen van de bestaande branch).
[Fact]
public async Task CreateShadowDocBranchAsync_WhenLocalBranchAlreadyExists_ThrowsInvalidOperationException()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/main")).ReturnsAsync("  docs/main  ");
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/main")).ReturnsAsync(string.Empty);

    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateShadowDocBranchAsync());
}

// Verifieert dat een InvalidOperationException wordt gegooid wanneer de shadow branch al remote bestaat.
// 'ls-remote --heads origin docs/main' retourneert de ref wanneer de branch op de remote aanwezig is.
// Ook in dit geval gooit de service een uitzondering om conflicten te voorkomen.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenRemoteBranchAlreadyExists_ThrowsInvalidOperationException()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/main")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/main")).ReturnsAsync("refs/heads/docs/main");

    await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.CreateShadowDocBranchAsync());
}

// Verifieert dat de huidige branchnaam wordt getrimd vóór gebruik.
// 'git rev-parse --abbrev-ref HEAD' geeft regelmatig een trailing newline ('\n') of spaties terug.
// Zonder .Trim() zou de shadow branch naam 'docs/  feature/login  \n' worden,
// waardoor 'branch --list' en 'checkout -b' de verkeerde branchnaam zouden ontvangen.
[Fact]
public async Task CreateShadowDocBranchAsync_CurrentBranchNameIsTrimmed()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("  feature/login  \n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/feature/login")).ReturnsAsync(string.Empty);

    var result = await _sut.CreateShadowDocBranchAsync();

    Assert.Equal("docs/feature/login", result);
}

// Verifieert dat 'git checkout -b' ook wordt gebruikt wanneer 'branch --list'
// alleen whitespace retourneert (behandeld als niet-bestaand).
// Dit dekt het geval waar het git-commando onverwacht whitespace teruggeeft in plaats van een lege string.
// De service gebruikt 'IsNullOrWhiteSpace' in plaats van 'IsNullOrEmpty' om dit op te vangen.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenBranchListIsWhitespace_CreatesNewBranch()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "branch --list docs/main")).ReturnsAsync("   ");
    _mockRunner.Setup(r => r.RunAsync("git", "ls-remote --heads origin docs/main")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/main")).ReturnsAsync(string.Empty);

    await _sut.CreateShadowDocBranchAsync();

    _mockRunner.Verify(r => r.RunAsync("git", "checkout -b docs/main"), Times.Once);
}

    // ── CommitAndPushAsync ─────────────────────────────────────────────────────

    // Verifieert dat 'git add -A' wordt aangeroepen om alle wijzigingen te stagen.
    // '-A' staat voor '--all': het staged nieuwe, gewijzigde én verwijderde bestanden in de hele repo.
    // Dit is de eerste stap vóór de commit.
    [Fact]
    public async Task CommitAndPushAsync_StagesAllChanges_RunsGitAddA()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "add -A")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "status --porcelain")).ReturnsAsync("M src/Foo.cs");
        _mockRunner.Setup(r => r.RunAsync("git", "commit -m \"docs: update\"")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
        _mockRunner.Setup(r => r.RunAsync("git", "push --set-upstream origin main")).ReturnsAsync(string.Empty);

        await _sut.CommitAndPushAsync("docs: update");

        _mockRunner.Verify(r => r.RunAsync("git", "add -A"), Times.Once);
    }

    // Verifieert dat commit en push worden overgeslagen wanneer 'git status --porcelain' leeg is.
    // 'status --porcelain' geeft een lege string terug wanneer er niets te committen valt.
    // In dat geval stopt de service vroeg om een lege commit te voorkomen.
    [Fact]
    public async Task CommitAndPushAsync_WhenNothingToCommit_SkipsCommitAndPush()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "add -A")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "status --porcelain")).ReturnsAsync(string.Empty);

        await _sut.CommitAndPushAsync("docs: update");

        _mockRunner.Verify(r => r.RunAsync("git", It.Is<string>(a => a.StartsWith("commit"))), Times.Never);
        _mockRunner.Verify(r => r.RunAsync("git", It.Is<string>(a => a.StartsWith("push"))), Times.Never);
    }

    // Verifieert dat 'git commit -m "<message>"' wordt aangeroepen met het opgegeven bericht.
    // Het commit-bericht wordt tussen aanhalingstekens geplaatst zodat spaties correct worden doorgegeven.
    [Fact]
    public async Task CommitAndPushAsync_CommitsWithGivenMessage_RunsGitCommit()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "add -A")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "status --porcelain")).ReturnsAsync("M src/Foo.cs");
        _mockRunner.Setup(r => r.RunAsync("git", "commit -m \"docs: update\"")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
        _mockRunner.Setup(r => r.RunAsync("git", "push --set-upstream origin main")).ReturnsAsync(string.Empty);

        await _sut.CommitAndPushAsync("docs: update");

        _mockRunner.Verify(r => r.RunAsync("git", "commit -m \"docs: update\""), Times.Once);
    }

    // Verifieert dat 'git push --set-upstream origin <branch>' wordt aangeroepen met de juiste branchnaam.
    // '--set-upstream' (of '-u') koppelt de lokale branch aan de remote tracking branch,
    // zodat toekomstige 'git push' en 'git pull' zonder extra argumenten werken.
    [Fact]
    public async Task CommitAndPushAsync_PushesWithSetUpstreamToOrigin_RunsGitPush()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "add -A")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "status --porcelain")).ReturnsAsync("M src/Foo.cs");
        _mockRunner.Setup(r => r.RunAsync("git", "commit -m \"docs: update\"")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("feature/login\n");
        _mockRunner.Setup(r => r.RunAsync("git", "push --set-upstream origin feature/login")).ReturnsAsync(string.Empty);

        await _sut.CommitAndPushAsync("docs: update");

        _mockRunner.Verify(r => r.RunAsync("git", "push --set-upstream origin feature/login"), Times.Once);
    }

    // Verifieert dat de branchnaam wordt getrimd vóór gebruik in de push-opdracht.
    // 'git rev-parse --abbrev-ref HEAD' geeft vaak een trailing newline of spaties terug.
    // Zonder .Trim() zou de push-opdracht 'push --set-upstream origin main\n' worden,
    // wat een ongeldige remote-branchnaam oplevert.
    [Fact]
    public async Task CommitAndPushAsync_BranchNameIsTrimmed_PushUsesCleanBranchName()
    {
        _mockRunner.Setup(r => r.RunAsync("git", "add -A")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "status --porcelain")).ReturnsAsync("M src/Foo.cs");
        _mockRunner.Setup(r => r.RunAsync("git", "commit -m \"docs: update\"")).ReturnsAsync(string.Empty);
        _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("  main  \n");
        _mockRunner.Setup(r => r.RunAsync("git", "push --set-upstream origin main")).ReturnsAsync(string.Empty);

        await _sut.CommitAndPushAsync("docs: update");

        _mockRunner.Verify(r => r.RunAsync("git", "push --set-upstream origin main"), Times.Once);
    }

    // ── CreatePullRequestAsync ─────────────────────────────────────────────────

    // Verifieert dat 'gh pr create' wordt aangeroepen met de juiste --base, --head, --title en --body argumenten.
    // 'gh pr create' is het GitHub CLI-commando om een pull request aan te maken.
    // '--base' is de doelbranch (de feature branch), '--head' is de bronbranch (de docs shadow branch).
    [Fact]
    public async Task CreatePullRequestAsync_CallsGhPrCreateWithCorrectArguments()
    {
        var expectedArgs = "pr create --base \"feature/login\" --head \"docs/feature/login\" --title \"docs: auto-generated documentation\" --body \"Auto-generated documentation via tool.\"";
        _mockRunner.Setup(r => r.RunAsync("gh", expectedArgs)).ReturnsAsync(string.Empty);

        await _sut.CreatePullRequestAsync("docs/feature/login", "feature/login", "docs: auto-generated documentation");

        _mockRunner.Verify(r => r.RunAsync("gh", expectedArgs), Times.Once);
    }

    // Verifieert dat de --base parameter de targetBranch bevat.
    // De targetBranch is de originele feature branch waar de PR naar toe gemerged wordt.
    [Fact]
    public async Task CreatePullRequestAsync_UsesTargetBranchAsBase()
    {
        _mockRunner.Setup(r => r.RunAsync("gh", It.IsAny<string>())).ReturnsAsync(string.Empty);

        await _sut.CreatePullRequestAsync("docs/main", "main", "docs: update");

        _mockRunner.Verify(r => r.RunAsync("gh", It.Is<string>(args => args.Contains("--base \"main\""))), Times.Once);
    }

    // Verifieert dat de --head parameter de docBranch bevat.
    // De docBranch is de shadow branch met de gegenereerde documentatie (bv. 'docs/feature/login').
    [Fact]
    public async Task CreatePullRequestAsync_UsesDocBranchAsHead()
    {
        _mockRunner.Setup(r => r.RunAsync("gh", It.IsAny<string>())).ReturnsAsync(string.Empty);

        await _sut.CreatePullRequestAsync("docs/feature/login", "feature/login", "docs: update");

        _mockRunner.Verify(r => r.RunAsync("gh", It.Is<string>(args => args.Contains("--head \"docs/feature/login\""))), Times.Once);
    }

    // Verifieert dat de --title parameter de opgegeven titel bevat.
    // De titel wordt tussen aanhalingstekens geplaatst zodat spaties correct worden doorgegeven aan de CLI.
    [Fact]
    public async Task CreatePullRequestAsync_UsesTitleInPrCommand()
    {
        _mockRunner.Setup(r => r.RunAsync("gh", It.IsAny<string>())).ReturnsAsync(string.Empty);

        await _sut.CreatePullRequestAsync("docs/main", "main", "docs: mijn pr titel");

        _mockRunner.Verify(r => r.RunAsync("gh", It.Is<string>(args => args.Contains("--title \"docs: mijn pr titel\""))), Times.Once);
    }

    // Verifieert dat de --body parameter de vaste auto-generated omschrijving bevat.
    // De body is een statische tekst die aangeeft dat de documentatie automatisch is gegenereerd.
    [Fact]
    public async Task CreatePullRequestAsync_IncludesAutoGeneratedBody()
    {
        _mockRunner.Setup(r => r.RunAsync("gh", It.IsAny<string>())).ReturnsAsync(string.Empty);

        await _sut.CreatePullRequestAsync("docs/main", "main", "docs: update");

        _mockRunner.Verify(r => r.RunAsync("gh", It.Is<string>(args => args.Contains("--body \"Auto-generated documentation via tool.\""))), Times.Once);
    }
}