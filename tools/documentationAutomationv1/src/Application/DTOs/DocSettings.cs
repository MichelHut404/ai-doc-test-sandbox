namespace src.Application.DTOs;

public class DocSettings
{
    public string Language { get; set; } = "csharp";
    public List<string> Exclude { get; set; } = new();
}