namespace LlmsTxtSynthesizer;

// Shared data models used by multiple generators

internal class Section
{
    public string? Name { get; set; } // Null = no section header (ungrouped items)
    public string? Description { get; set; } // Optional description shown after section header
    public List<(string Title, string Url)> Links { get; set; } = new();
}
