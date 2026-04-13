namespace src.Infrastructure.Interfaces;

public interface ICMDProcessRunner
{
    Task<string> RunAsync(string command, string arguments);
}