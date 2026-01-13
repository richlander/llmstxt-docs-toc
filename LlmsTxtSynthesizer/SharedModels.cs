namespace LlmsTxtSynthesizer;

// Shared data models used by multiple generators

internal class Section
{
    public string? Name { get; set; } // Null = no section header (ungrouped items)
    public string? Description { get; set; } // Optional description shown after section header
    public List<(string Title, string Url)> Links { get; set; } = new();
}

// YAML data models for toc.yml structure
public class TocRoot
{
    public List<TocItem> Items { get; set; } = new();
}

public class TocItem
{
    public string? Name { get; set; }
    public string? Href { get; set; }
    public string? TocHref { get; set; }
    public string? DisplayName { get; set; }
    public List<TocItem>? Items { get; set; }
    public bool? Expanded { get; set; }
}
