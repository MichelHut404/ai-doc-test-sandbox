using System.Diagnostics;
using src.Infrastructure.Interfaces;

namespace src.Infrastructure.Services;

public class CMDProcessRunner : IProcessRunner
{
    public async Task<string> RunAsync(string command, string arguments)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        var output = await outputTask;
        var error = await errorTask;

        if (process.ExitCode != 0)
            throw new InvalidOperationException(
                $"'{command} {arguments}' failed (exit {process.ExitCode}).\n{error.Trim()}");

        return output;
    }
}