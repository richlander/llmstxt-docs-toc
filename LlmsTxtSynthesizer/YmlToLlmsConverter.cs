using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Converts toc.yml files to llms-{topic}.txt format.
/// Handles hierarchical YAML structures and flattens to curated links within 50-line limit.
/// </summary>
public class YmlToLlmsConverter
{
    private readonly int _maxLines;
    private readonly int _maxLinksPerFile;
    private readonly string _githubBaseUrl;

    public YmlToLlmsConverter(int maxLines = 50, int maxLinksPerFile = 30, string githubBaseUrl = "https://raw.githubusercontent.com/dotnet/docs/refs/heads/llmstxt")
    {
        _maxLines = maxLines;
        _maxLinksPerFile = maxLinksPerFile;
        _githubBaseUrl = githubBaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Convert all toc.yml files in a directory tree to llms.txt files.
    /// </summary>
    public int ConvertAllInDirectory(string targetDir, bool dryRun = false)
    {
        if (!Directory.Exists(targetDir))
        {
            throw new DirectoryNotFoundException($"Directory not found: {targetDir}");
        }

        var tocFiles = Directory.GetFiles(targetDir, "toc.yml", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        Console.WriteLine($"Found {tocFiles.Count} toc.yml files\n");

        var converted = 0;
        foreach (var tocFile in tocFiles)
        {
            var relPath = Path.GetRelativePath(targetDir, tocFile);
            Console.WriteLine($"Processing: {relPath}");

            try
            {
                var outputPath = ConvertFile(tocFile, dryRun);
                if (outputPath != null)
                {
                    var outputRel = Path.GetRelativePath(targetDir, outputPath);
                    if (dryRun)
                    {
                        Console.WriteLine($"  ⊘ Would create: {outputRel}");
                    }
                    else
                    {
                        var lineCount = File.ReadAllLines(outputPath).Length;
                        Console.WriteLine($"  ✓ Created: {outputRel} ({lineCount} lines)");
                    }
                    converted++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }

        Console.WriteLine($"\nConverted {converted}/{tocFiles.Count} files");
        return converted;
    }

    /// <summary>
    /// Convert a single toc.yml file to llms-{topic}.txt.
    /// Returns the output file path, or null if conversion was skipped.
    /// </summary>
    public string? ConvertFile(string tocYmlPath, bool dryRun = false)
    {
        if (!File.Exists(tocYmlPath))
        {
            throw new FileNotFoundException($"File not found: {tocYmlPath}");
        }

        var yaml = File.ReadAllText(tocYmlPath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        TocRoot? tocRoot;
        try
        {
            tocRoot = deserializer.Deserialize<TocRoot>(yaml);
        }
        catch
        {
            // Try as direct items array
            try
            {
                var items = deserializer.Deserialize<List<TocItem>>(yaml);
                tocRoot = new TocRoot { Items = items };
            }
            catch
            {
                return null;
            }
        }

        if (tocRoot?.Items == null || tocRoot.Items.Count == 0)
        {
            return null;
        }

        // Extract hierarchical structure preserving categories
        var sections = ExtractSections(tocRoot.Items);

        if (sections.Count == 0)
        {
            return null;
        }

        // Generate llms.txt content
        var dir = Path.GetDirectoryName(tocYmlPath)!;
        var dirName = Path.GetFileName(dir);
        var title = GenerateTitle(dirName, tocRoot);
        
        var content = GenerateGroupedContent(title, sections, dir);

        // Output filename is always llms.txt
        var outputPath = Path.Combine(dir, "llms.txt");

        if (!dryRun)
        {
            File.WriteAllText(outputPath, content);
        }

        return outputPath;
    }

    private void FlattenItems(List<TocItem> items, List<(string Title, string Url, int Depth)> links, int depth)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                var url = item.Href ?? item.TocHref ?? "";
                links.Add((item.Name, url, depth));

                // Recursively process nested items
                if (item.Items != null && item.Items.Count > 0)
                {
                    FlattenItems(item.Items, links, depth + 1);
                }
            }
        }
    }

    private List<Section> ExtractSections(List<TocItem> items)
    {
        var sections = new List<Section>();

        foreach (var item in items)
        {
            // Top-level items with children become sections
            if (item.Items != null && item.Items.Count > 0)
            {
                var section = new Section
                {
                    Name = item.Name ?? "Docs",
                    Links = new List<(string Title, string Url)>()
                };

                // Collect all child links (flatten nested children)
                CollectLinks(item.Items, section.Links);
                
                if (section.Links.Any())
                {
                    sections.Add(section);
                }
            }
            // Top-level items without children go into a null-named section (no header)
            else if (!string.IsNullOrEmpty(item.Name))
            {
                var url = item.Href ?? item.TocHref ?? "";
                if (!string.IsNullOrEmpty(url))
                {
                    // Find or create section with null name for ungrouped links
                    var ungroupedSection = sections.FirstOrDefault(s => s.Name == null);
                    if (ungroupedSection == null)
                    {
                        ungroupedSection = new Section { Name = null, Links = new List<(string Title, string Url)>() };
                        sections.Insert(0, ungroupedSection); // Put at beginning
                    }
                    ungroupedSection.Links.Add((item.Name, url));
                }
            }
        }

        return sections;
    }

    private void CollectLinks(List<TocItem> items, List<(string Title, string Url)> links)
    {
        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item.Name))
            {
                var url = item.Href ?? item.TocHref ?? "";
                if (!string.IsNullOrEmpty(url))
                {
                    links.Add((item.Name, url));
                }

                // Recursively collect from nested items
                if (item.Items != null && item.Items.Count > 0)
                {
                    CollectLinks(item.Items, links);
                }
            }
        }
    }

    private string GenerateTitle(string dirName, TocRoot tocRoot)
    {
        // Use directory name + "Docs" (e.g., "Interop Docs", "Claude Code Docs")
        return $"{ConvertToTitleCase(dirName)} Docs";
    }

    private string GenerateSummary(string dirName, int linkCount)
    {
        return $"Documentation for {ConvertToTitleCase(dirName)} ({linkCount} topics)";
    }

    private string GenerateContent(string title, string summary, List<(string Title, string Url, int Depth)> links, string baseDir)
    {
        var lines = new List<string>
        {
            $"# {title}",
            "",
            $"> {summary}",
            ""
        };

        // Flatten to single section - sort by semantic keywords for readability
        var allLinks = links
            .OrderBy(l => GetSortKey(l.Title))
            .Take(_maxLinksPerFile)
            .ToList();

        if (allLinks.Any())
        {
            lines.Add("## Docs");
            lines.Add("");
            foreach (var link in allLinks.Take(_maxLines - lines.Count - 1))
            {
                lines.Add(FormatLink(link.Title, link.Url, baseDir));
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private string GenerateGroupedContent(string title, List<Section> sections, string baseDir)
    {
        var lines = new List<string>
        {
            $"# {title}",
            ""
        };

        int totalLinks = sections.Sum(s => s.Links.Count);
        int estimatedLines = 2 + (sections.Count * 3) + totalLinks; // title + blank + (section header + blank + links)

        // If we're over limit, need to truncate
        int availableLines = _maxLines - 2; // Reserve for title and blank line
        
        foreach (var section in sections)
        {
            // Check if we have room for section header (if present) + at least one link
            int headerLines = section.Name != null ? 2 : 0; // header + blank line
            if (lines.Count + headerLines + 1 > _maxLines)
            {
                break;
            }

            // Only add section header if section has a name
            if (section.Name != null)
            {
                lines.Add($"## {section.Name}");
                lines.Add("");
            }

            foreach (var (linkTitle, linkUrl) in section.Links)
            {
                // Leave room for potential next section
                if (lines.Count + 1 >= _maxLines)
                {
                    break;
                }
                
                lines.Add(FormatLink(linkTitle, linkUrl, baseDir));
            }

            // Add blank line between sections (but not after the last one)
            if (section != sections.Last() && lines.Count + 2 < _maxLines)
            {
                lines.Add("");
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private string GetSortKey(string title)
    {
        // Remove common prefixes and filler words for more semantic sorting
        var sortKey = title;
        
        // Remove common product/framework prefixes
        var prefixes = new[] { ".NET Framework", ".NET", "ASP.NET Core", "ASP.NET", "EF Core" };
        foreach (var prefix in prefixes)
        {
            if (sortKey.StartsWith(prefix + " ", StringComparison.OrdinalIgnoreCase))
            {
                sortKey = sortKey.Substring(prefix.Length).TrimStart();
                break;
            }
        }
        
        // Remove common prepositions and articles at the start
        var fillers = new[] { "How to: ", "with ", "for ", "in ", "on ", "via ", "from ", "to ", "using ", "the ", "a ", "an " };
        foreach (var filler in fillers)
        {
            if (sortKey.StartsWith(filler, StringComparison.OrdinalIgnoreCase))
            {
                sortKey = sortKey.Substring(filler.Length).TrimStart();
                break;
            }
        }
        
        return sortKey;
    }

    private string FormatLink(string title, string url, string baseDir)
    {
        // Clean up the title
        var cleanTitle = title.Trim();
        
        // If URL is empty, skip the URL part
        if (string.IsNullOrEmpty(url))
        {
            return $"- {cleanTitle}";
        }

        // Convert to GitHub raw URL
        var githubUrl = ConvertToGitHubUrl(url, baseDir);

        // Try to extract description from the markdown file
        var description = ExtractDescriptionFromMarkdown(url, baseDir);
        
        if (!string.IsNullOrEmpty(description))
        {
            return $"- [{cleanTitle}]({githubUrl}): {description}";
        }

        return $"- [{cleanTitle}]({githubUrl})";
    }

    private string ConvertToGitHubUrl(string url, string baseDir)
    {
        // If already a full URL (http/https), return as-is
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        // Remove query parameters and fragments for the URL (keep for description lookup)
        var cleanUrl = url;
        var questionMarkIndex = cleanUrl.IndexOf('?');
        if (questionMarkIndex > 0)
        {
            cleanUrl = cleanUrl.Substring(0, questionMarkIndex);
        }
        
        var hashIndex = cleanUrl.IndexOf('#');
        if (hashIndex > 0)
        {
            cleanUrl = cleanUrl.Substring(0, hashIndex);
        }

        // Resolve relative path to full path
        string fullPath;
        if (!Path.IsPathRooted(cleanUrl))
        {
            fullPath = Path.GetFullPath(Path.Combine(baseDir, cleanUrl));
        }
        else
        {
            fullPath = cleanUrl;
        }

        // Find git root by looking for .git directory
        var gitRoot = FindGitRoot(baseDir);
        if (gitRoot == null)
        {
            // Fallback: return relative URL if no git root found
            return cleanUrl;
        }

        // Get path relative to git root
        var repoRelativePath = Path.GetRelativePath(gitRoot, fullPath);

        // Construct GitHub raw URL
        return $"{_githubBaseUrl}/{repoRelativePath}";
    }

    private string? FindGitRoot(string startPath)
    {
        var currentDir = new DirectoryInfo(startPath);
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            {
                return currentDir.FullName;
            }
            currentDir = currentDir.Parent;
        }
        return null;
    }

    private string ExtractDescriptionFromMarkdown(string url, string baseDir)
    {
        // Handle various URL formats
        var filePath = url;
        
        // Skip non-markdown files
        if (!filePath.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        // Remove query parameters and fragments
        var questionMarkIndex = filePath.IndexOf('?');
        if (questionMarkIndex > 0)
        {
            filePath = filePath.Substring(0, questionMarkIndex);
        }
        
        var hashIndex = filePath.IndexOf('#');
        if (hashIndex > 0)
        {
            filePath = filePath.Substring(0, hashIndex);
        }

        // Resolve relative paths
        if (!Path.IsPathRooted(filePath))
        {
            filePath = Path.Combine(baseDir, filePath);
        }

        try
        {
            if (!File.Exists(filePath))
            {
                return string.Empty;
            }

            // Read first ~30 lines to find YAML frontmatter
            var lines = File.ReadLines(filePath).Take(30).ToList();
            
            // Look for YAML frontmatter (between --- markers)
            var inFrontmatter = false;
            var descriptionPrefix = "description:";
            
            foreach (var line in lines)
            {
                if (line.Trim() == "---")
                {
                    if (!inFrontmatter)
                    {
                        inFrontmatter = true;
                        continue;
                    }
                    else
                    {
                        // End of frontmatter
                        break;
                    }
                }
                
                if (inFrontmatter && line.TrimStart().StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    // Extract description value
                    var descLine = line.Substring(line.IndexOf(descriptionPrefix) + descriptionPrefix.Length).Trim();
                    
                    // Remove quotes if present
                    if (descLine.StartsWith("\"") && descLine.EndsWith("\""))
                    {
                        descLine = descLine.Substring(1, descLine.Length - 2);
                    }
                    
                    return descLine;
                }
            }
        }
        catch
        {
            // If we can't read the file, just return empty
            return string.Empty;
        }

        return string.Empty;
    }

    private string ConvertToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "Documentation";
        }

        var words = input.Split('-', '_', ' ')
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w => char.ToUpper(w[0]) + w.Substring(1).ToLower());

        return string.Join(" ", words);
    }

    private string SanitizeName(string name)
    {
        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");
    }
}
