using src.Application.DTOs;

namespace documentationAutomationv1.Application.Interfaces;

public interface IPromptBuilder
{
    DocumentationType DocumentationType { get; }
    string Build(string filesSections);
}
