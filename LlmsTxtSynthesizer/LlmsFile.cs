namespace LlmsTxtSynthesizer;

/// <summary>
/// Represents a link with its description and metadata.
/// </summary>
public record Link(
    string Url,
    string Description,
    string SourceFile,
    string Section = "",
    int Priority = 0)
{
    public override string ToString() => $"- [{Description}]({Url})";
}

/// <summary>
/// Represents an llms.txt file with its content and metadata.
/// </summary>
public class LlmsFile
{
    public string Path { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public Dictionary<string, List<Link>> Sections { get; set; } = new();
    public string? ParentLink { get; set; }
    public int LineCount { get; set; }
    
    public string GetRelativePath(string basePath)
    {
        try
        {
            return System.IO.Path.GetRelativePath(basePath, Path);
        }
        catch
        {
            return Path;
        }
    }
}
