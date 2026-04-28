using src.Application.DTOs;

namespace documentationAutomationv1.Application.Interfaces;

public interface ISettingsService
{
    DocSettings LoadSettings();
    bool IsExcluded(string filePath, string gitRoot, string pattern);

}