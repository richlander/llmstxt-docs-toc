using System.Text.Json.Serialization;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Models for _llms.json customization files.
/// These files customize llms.txt generation with overrides for titles,
/// descriptions, guidance, and structural adjustments.
/// </summary>

public class LlmsCustomization
{
    [JsonPropertyName("$schema")]
    public string? Schema { get; set; }

    /// <summary>
    /// Override the section/document title. Falls back to toc.yml name or .md frontmatter.
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Override description text (rendered as blockquote under H1/H2). Falls back to .md frontmatter.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Short description for use in link text (e.g., in Subtopics).
    /// </summary>
    [JsonPropertyName("shortDescription")]
    public string? ShortDescription { get; set; }

    /// <summary>
    /// Important preamble text (warnings, cautions) rendered prominently after description.
    /// </summary>
    [JsonPropertyName("preamble")]
    public string? Preamble { get; set; }

    /// <summary>
    /// Structured guidance section for AI assistants.
    /// </summary>
    [JsonPropertyName("guidance")]
    public GuidanceSection? Guidance { get; set; }

    /// <summary>
    /// Ordered list of high-value child paths to offer to parent (max 6).
    /// First = most important.
    /// </summary>
    [JsonPropertyName("offers")]
    public List<string>? Offers { get; set; }

    /// <summary>
    /// Define sections with explicit includes or child references.
    /// </summary>
    [JsonPropertyName("sections")]
    public List<SectionDefinition>? Sections { get; set; }

    /// <summary>
    /// Per-node overrides keyed by relative path.
    /// </summary>
    [JsonPropertyName("nodes")]
    public Dictionary<string, NodeOverride>? Nodes { get; set; }

    /// <summary>
    /// Paths to exclude from output.
    /// </summary>
    [JsonPropertyName("filter")]
    public List<string>? Filter { get; set; }

    /// <summary>
    /// Nodes to promote up the hierarchy.
    /// </summary>
    [JsonPropertyName("promote")]
    public List<PromoteRule>? Promote { get; set; }

    /// <summary>
    /// Cross-references to related topics.
    /// </summary>
    [JsonPropertyName("related")]
    public List<RelatedTopic>? Related { get; set; }
}

public class GuidanceSection
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("intro")]
    public string? Intro { get; set; }

    [JsonPropertyName("items")]
    public List<string>? Items { get; set; }
}

public class SectionDefinition
{
    /// <summary>
    /// Section name (use when creating a new grouping).
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Path to child (child's _llms.json provides title/description).
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// Sort order - higher values appear first.
    /// </summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// Description for this section (shown after section header).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Specific child nodes the parent wants included.
    /// </summary>
    [JsonPropertyName("wants")]
    public List<string>? Wants { get; set; }

    /// <summary>
    /// Explicit list of paths to include in this section.
    /// </summary>
    [JsonPropertyName("include")]
    public List<string>? Include { get; set; }
}

public class NodeOverride
{
    /// <summary>
    /// Override display name (falls back to toc.yml name or .md frontmatter title).
    /// </summary>
    [JsonPropertyName("rename")]
    public string? Rename { get; set; }

    /// <summary>
    /// Override description (falls back to .md frontmatter description).
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

public class PromoteRule
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("levels")]
    public int Levels { get; set; } = 1;
}

public class RelatedTopic
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("weight")]
    public double? Weight { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }

    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}
