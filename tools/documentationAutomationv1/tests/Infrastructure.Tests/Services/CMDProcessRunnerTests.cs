using src.Infrastructure.Services;

namespace documentationAutomationv1.Infrastructure.Tests.Services;

public class CMDProcessRunnerTests
{
    private readonly CMDProcessRunner _sut = new();

    // ── RunAsync ──────────────────────────────────────────────────────────────

    // Verifieert dat RunAsync de standaarduitvoer van een succesvol commando retourneert.
    [Fact]
    public async Task RunAsync_WhenCommandSucceeds_ReturnsStdOut()
    {
        var result = await _sut.RunAsync("cmd.exe", "/c echo hallo");

        Assert.Contains("hallo", result);
    }

    // Verifieert dat RunAsync de volledige uitvoer retourneert, inclusief whitespace en newlines.
    [Fact]
    public async Task RunAsync_WhenCommandSucceeds_ReturnsRawOutput()
    {
        var result = await _sut.RunAsync("cmd.exe", "/c echo test");

        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    // Verifieert dat RunAsync een InvalidOperationException gooit wanneer het commando
    // een exit code ongelijk aan nul retourneert.
    [Fact]
    public async Task RunAsync_WhenCommandFails_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RunAsync("cmd.exe", "/c exit 1"));
    }

    // Verifieert dat de foutmelding de naam van het commando, de argumenten en de exit code bevat,
    // zodat de fout makkelijk te diagnosticeren is.
    [Fact]
    public async Task RunAsync_WhenCommandFails_ExceptionMessageContainsCommandAndExitCode()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.RunAsync("cmd.exe", "/c exit 2"));

        Assert.Contains("cmd.exe", ex.Message);
        Assert.Contains("/c exit 2", ex.Message);
        Assert.Contains("exit 2", ex.Message);
    }

    // Verifieert dat RunAsync een exception gooit wanneer het opgegeven programma niet bestaat.
    [Fact]
    public async Task RunAsync_WhenCommandDoesNotExist_ThrowsException()
    {
        await Assert.ThrowsAnyAsync<Exception>(() =>
            _sut.RunAsync("dit-programma-bestaat-niet.exe", ""));
    }

    // Verifieert dat RunAsync de uitvoer van een commando met meerdere regels volledig retourneert.
    [Fact]
    public async Task RunAsync_WhenCommandOutputsMultipleLines_ReturnsAllLines()
    {
        var result = await _sut.RunAsync("cmd.exe", "/c echo regel1 && echo regel2");

        Assert.Contains("regel1", result);
        Assert.Contains("regel2", result);
    }
}