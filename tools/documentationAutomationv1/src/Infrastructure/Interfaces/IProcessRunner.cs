namespace src.Infrastructure.Interfaces;

public interface IProcessRunner
{
    Task<string> RunAsync(string command, string arguments);
}