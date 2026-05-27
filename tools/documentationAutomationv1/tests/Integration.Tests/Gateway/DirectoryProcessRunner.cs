using System.Diagnostics;
using src.Infrastructure.Interfaces;

namespace documentationAutomationv1.Integration.Tests.Gateway;

/// IProcessRunner implementatie voor integratie tests met een vaste working directory.
/// Nodig omdat CmdProcessRunner geen WorkingDirectory ondersteunt.
public class DirectoryProcessRunner : IProcessRunner
{
    private readonly string _workingDirectory;

    public DirectoryProcessRunner(string workingDirectory)
    {
        _workingDirectory = workingDirectory;
    }

    public async Task<string> RunAsync(string command, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = _workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }
}