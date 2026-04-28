namespace src.Application.DTOs;

public class DocSettings
{
    public required string languageFileExtension { get; set; }
    public List<string> Exclude { get; set; } = new();
}