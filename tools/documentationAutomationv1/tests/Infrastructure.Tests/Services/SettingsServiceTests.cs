using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class SettingsServiceTests
{
    private readonly SettingsService _sut = new();

    // ── IsExcluded ────────────────────────────────────────────────────────────

    // Verifieert dat een bestand dat overeenkomt met het glob-patroon als excluded wordt beschouwd.
    [Fact]
    public void IsExcluded_WhenFileMatchesPattern_ReturnsTrue()
    {
        var gitRoot = @"C:\repo";
        var filePath = @"C:\repo\src\Generated\Foo.cs";
        var pattern = "src/Generated/**";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.True(result);
    }

    // Verifieert dat een bestand dat niet overeenkomt met het glob-patroon niet excluded is.
    [Fact]
    public void IsExcluded_WhenFileDoesNotMatchPattern_ReturnsFalse()
    {
        var gitRoot = "C:/repo/";
        var filePath = "C:/repo/src/Services/MyService.cs";
        var pattern = "src/Generated/**";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.False(result);
    }

    // Verifieert dat backslashes in het bestandspad correct genormaliseerd worden naar forward slashes
    // zodat de glob-matcher platformonafhankelijk werkt.
    [Fact]
    public void IsExcluded_WithBackslashesInFilePath_NormalizesAndMatchesCorrectly()
    {
        var gitRoot = @"C:\repo";
        var filePath = @"C:\repo\src\Generated\Foo.cs";
        var pattern = "src/Generated/**";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.True(result);
    }

    // Verifieert dat een exact bestandsnaampatroon werkt (zonder wildcard).
    [Fact]
    public void IsExcluded_WithExactFilePattern_MatchesCorrectly()
    {
        var gitRoot = @"C:\repo";
        var filePath = @"C:\repo\Program.cs";
        var pattern = "Program.cs";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.True(result);
    }

    // Verifieert dat een trailing slash op gitRoot correct wordt afgehandeld zonder dubbele slashes.
    [Fact]
    public void IsExcluded_WhenGitRootHasTrailingSlash_DoesNotBreakMatching()
    {
        var gitRoot = @"C:\repo\";
        var filePath = @"C:\repo\src\Foo.cs";
        var pattern = "src/*.cs";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.True(result);
    }

    // Verifieert dat wanneer het bestandspad niet begint met de gitRoot, het pad zelf als relatief pad
    // wordt gebruikt voor de matcher, zodat er geen crash optreedt.
    [Fact]
    public void IsExcluded_WhenFilePathDoesNotStartWithGitRoot_UsesFilePathDirectly()
    {
        var gitRoot = "C:/other-repo/";
        var filePath = "src/Foo.cs";
        var pattern = "src/*.cs";

        var result = _sut.IsExcluded(filePath, gitRoot, pattern);

        Assert.True(result);
    }

    // ── LoadSettings ──────────────────────────────────────────────────────────

    // Verifieert dat LoadSettings een geldig DocSettings-object retourneert wanneer een
    // docsettings.json aanwezig is in de huidige werkdirectory.
    [Fact]
    public void LoadSettings_WhenSettingsFileExistsInCurrentDirectory_ReturnsDocSettings()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "docsettings.json"),
            """{"languageFileExtension": "cs", "Exclude": []}""");

        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var result = _sut.LoadSettings();

            Assert.Equal("cs", result.languageFileExtension);
            Assert.Empty(result.Exclude);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(tempDir, true);
        }
    }

    // Verifieert dat LoadSettings een FileNotFoundException gooit wanneer er geen docsettings.json
    // gevonden kan worden in de directory-boom.
    [Fact]
    public void LoadSettings_WhenNoSettingsFileFound_ThrowsFileNotFoundException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // Maak een .git-map aan zodat de zoeklogica stopt bij deze directory.
        Directory.CreateDirectory(Path.Combine(tempDir, ".git"));

        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);

            Assert.Throws<FileNotFoundException>(() => _sut.LoadSettings());
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(tempDir, true);
        }
    }

    // Verifieert dat LoadSettings een exception gooit wanneer docsettings.json
    // ongeldig JSON bevat.
    [Fact]
    public void LoadSettings_WhenSettingsFileIsInvalid_ThrowsException()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "docsettings.json"), "niet geldig json {{{");

        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);

            Assert.ThrowsAny<Exception>(() => _sut.LoadSettings());
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(tempDir, true);
        }
    }

    // Verifieert dat LoadSettings de Exclude-lijst correct deserialiseert.
    [Fact]
    public void LoadSettings_WhenExcludeListPresent_DeserializesCorrectly()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "docsettings.json"),
            """{"languageFileExtension": "ts", "Exclude": ["**/bin/**", "**/obj/**"]}""");

        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var result = _sut.LoadSettings();

            Assert.Equal(2, result.Exclude.Count);
            Assert.Contains("**/bin/**", result.Exclude);
            Assert.Contains("**/obj/**", result.Exclude);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(tempDir, true);
        }
    }

    // Verifieert dat LoadSettings omhoog loopt in de directory-boom en docsettings.json
    // vindt in een bovenliggende map wanneer die niet in de huidige map staat.
    [Fact]
    public void LoadSettings_WhenSettingsFileExistsInParentDirectory_ReturnsDocSettings()
    {
        var parentDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var childDir = Path.Combine(parentDir, "submap");
        Directory.CreateDirectory(childDir);
        File.WriteAllText(Path.Combine(parentDir, "docsettings.json"),
            """{"languageFileExtension": "cs", "Exclude": []}""");

        var original = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(childDir);
            var result = _sut.LoadSettings();

            Assert.Equal("cs", result.languageFileExtension);
        }
        finally
        {
            Directory.SetCurrentDirectory(original);
            Directory.Delete(parentDir, true);
        }
    }
}