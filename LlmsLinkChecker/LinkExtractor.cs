using System.Text.RegularExpressions;

namespace LlmsLinkChecker;

/// <summary>
/// Parser for llms.txt files to extract links.
/// </summary>
public static partial class LinkExtractor
{
    [GeneratedRegex(@"\[([^\]]+)\]\(([^\)]+)\)")]
    private static partial Regex LinkPattern();

    /// <summary>
    /// Extracts all links from an llms.txt file content.
    /// </summary>
    public static IEnumerable<ExtractedLink> ExtractLinks(string content, string sourceFile)
    {
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            foreach (Match match in LinkPattern().Matches(line))
            {
                var title = match.Groups[1].Value;
                var url = match.Groups[2].Value;
                yield return new ExtractedLink(url, title, sourceFile, i + 1);
            }
        }
    }

    /// <summary>
    /// Determines if a URL points to an llms.txt file.
    /// </summary>
    public static bool IsLlmsTxtLink(string url)
    {
        return url.EndsWith("llms.txt", StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// Represents a link extracted from an llms.txt file.
/// </summary>
public record ExtractedLink(string Url, string Title, string SourceFile, int LineNumber);
