namespace documentationAutomationv1.Application.Interfaces;

public interface IGitService
{

    Task<IEnumerable<string>> GetChangedFilesAsync();
    Task<string> CreateShadowDocBranchAsync();
    Task CommitAndPushAsync(string message);

}
