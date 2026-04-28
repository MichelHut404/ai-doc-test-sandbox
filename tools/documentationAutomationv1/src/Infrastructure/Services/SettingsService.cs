using System.Text.Json;
using documentationAutomationv1.Application.Interfaces;
using Microsoft.Extensions.FileSystemGlobbing;
using src.Application.DTOs;

namespace src.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    public bool IsExcluded(string filePath, string gitRoot, string pattern)
    {
        var normalizedRoot = gitRoot.TrimEnd(Path.DirectorySeparatorChar, '/') + Path.DirectorySeparatorChar;
        var relativePath = filePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            ? filePath[normalizedRoot.Length..].Replace('\\', '/')
            : filePath.Replace('\\', '/');

        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        return matcher.Match(relativePath).HasMatches;
    }

    public DocSettings LoadSettings()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null)
        {
            var settingsFile = Path.Combine(dir.FullName, "docsettings.json");
            if (File.Exists(settingsFile))
            {
                var json = File.ReadAllText(settingsFile);
                return JsonSerializer.Deserialize<DocSettings>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? throw new InvalidOperationException($"'docsettings.json' at '{settingsFile}' is empty or invalid.");
            }

            if (dir.GetDirectories(".git").Length > 0)
                break;

            dir = dir.Parent;
        }
        throw new FileNotFoundException(
            "No 'docsettings.json' found. Add a docsettings.json to your repository root.");
    }
}