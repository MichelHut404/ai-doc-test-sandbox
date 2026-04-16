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
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/feature/login")).ReturnsAsync(string.Empty);

    var result = await _sut.CreateShadowDocBranchAsync();

    Assert.Equal("docs/feature/login", result);
}

// Verifieert dat 'git checkout -b' wordt gebruikt wanneer de shadow branch nog niet bestaat.
// 'git rev-parse --verify docs/main' controleert of de branch 'docs/main' bestaat.
// '--verify' valideert of de opgegeven ref (branch/commit) bestaat in de repository.
// Een lege string als response betekent dat de branch niet bestaat → 'checkout -b' maakt hem aan.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenBranchDoesNotExist_CreatesNewBranch()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify docs/main")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/main")).ReturnsAsync(string.Empty);

    await _sut.CreateShadowDocBranchAsync();

    _mockRunner.Verify(r => r.RunAsync("git", "checkout -b docs/main"), Times.Once);
}

// Verifieert dat 'git checkout' (zonder -b) wordt gebruikt wanneer de shadow branch al bestaat.
// 'rev-parse --verify docs/main' retourneert de commit-hash (bv. 'abc123') als de branch wél bestaat.
// In dat geval gebruikt de service 'git checkout docs/main' om er naar te switchen zonder opnieuw aan te maken.
// 'checkout -b' op een bestaande branch zou een fout geven, vandaar de splitsing.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenBranchAlreadyExists_ChecksOutExistingBranch()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify docs/main")).ReturnsAsync("abc123");
    _mockRunner.Setup(r => r.RunAsync("git", "checkout docs/main")).ReturnsAsync(string.Empty);

    await _sut.CreateShadowDocBranchAsync();

    _mockRunner.Verify(r => r.RunAsync("git", "checkout docs/main"), Times.Once);
    _mockRunner.Verify(r => r.RunAsync("git", "checkout -b docs/main"), Times.Never);
}

// Verifieert dat de huidige branchnaam wordt getrimd vóór gebruik.
// 'git rev-parse --abbrev-ref HEAD' geeft regelmatig een trailing newline ('\n') of spaties terug.
// Zonder .Trim() zou de shadow branch naam 'docs/  feature/login  \n' worden,
// waardoor 'rev-parse --verify' en 'checkout -b' de verkeerde branchnaam zouden ontvangen.
[Fact]
public async Task CreateShadowDocBranchAsync_CurrentBranchNameIsTrimmed()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("  feature/login  \n");
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify docs/feature/login")).ReturnsAsync(string.Empty);
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/feature/login")).ReturnsAsync(string.Empty);

    var result = await _sut.CreateShadowDocBranchAsync();

    Assert.Equal("docs/feature/login", result);
}

// Verifieert dat 'git checkout -b' ook wordt gebruikt wanneer 'rev-parse --verify'
// alleen whitespace retourneert (behandeld als niet-bestaand).
// Dit dekt het geval waar het git-commando onverwacht whitespace teruggeeft in plaats van een lege string.
// De service gebruikt 'IsNullOrWhiteSpace' in plaats van 'IsNullOrEmpty' om dit op te vangen.
[Fact]
public async Task CreateShadowDocBranchAsync_WhenBranchVerifyIsWhitespace_CreatesNewBranch()
{
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --abbrev-ref HEAD")).ReturnsAsync("main\n");
    _mockRunner.Setup(r => r.RunAsync("git", "rev-parse --verify docs/main")).ReturnsAsync("   ");
    _mockRunner.Setup(r => r.RunAsync("git", "checkout -b docs/main")).ReturnsAsync(string.Empty);

    await _sut.CreateShadowDocBranchAsync();

    _mockRunner.Verify(r => r.RunAsync("git", "checkout -b docs/main"), Times.Once);
}
}