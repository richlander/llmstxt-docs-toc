namespace LlmsTxtSynthesizer;

/// <summary>
/// Synthesizes a root llms.txt from child files.
/// </summary>
public class LlmsSynthesizer
{
    private readonly string _targetDir;
    private readonly int _maxLines;
    private readonly List<LlmsFile> _childFiles = new();
    
    public LlmsSynthesizer(string targetDir, int maxLines = 50)
    {
        _targetDir = targetDir;
        _maxLines = maxLines;
    }
    
    public List<string> DiscoverChildFiles()
    {
        var files = new List<string>();
        
        if (!Directory.Exists(_targetDir))
        {
            return files;
        }
        
        foreach (var file in Directory.EnumerateFiles(_targetDir, "llms.txt", SearchOption.AllDirectories))
        {
            var fileDir = Path.GetDirectoryName(file);
            
            // Skip the root llms.txt itself
            if (fileDir == _targetDir)
            {
                continue;
            }
            
            files.Add(file);
        }
        
        return files.OrderBy(f => f).ToList();
    }
    
    public void LoadChildFiles()
    {
        var childPaths = DiscoverChildFiles();
        
        foreach (var path in childPaths)
        {
            var parsed = LlmsParser.ParseFile(path);
            _childFiles.Add(parsed);
        }
    }
    
    public string GenerateRootContent(
        string title = ".NET Documentation",
        string summary = "Build applications for any platform with C#, F#, and Visual Basic.")
    {
        var lines = new List<string>();
        
        // Header (4 lines including blanks)
        lines.Add($"# {title}");
        lines.Add("");
        lines.Add($"> {summary}");
        lines.Add("");
        
        // Quick Start section (if we have getting-started file) - max 7 lines
        var gettingStarted = FindFileByPattern("getting-started");
        if (gettingStarted != null)
        {
            lines.Add("## Quick Start");
            lines.Add("");
            var topLinks = GetTopLinksFromFile(gettingStarted, limit: 3);
            foreach (var link in topLinks)
            {
                lines.Add(link.ToString());
            }
            lines.Add("");
        }
        
        // By Topic section (links to child files) - limit to stay under 50 lines total
        lines.Add("## By Topic");
        lines.Add("");
        
        // Reserve ~20 lines for topics, rest for other sections
        var maxTopicCount = Math.Min(_childFiles.Count, 15);
        foreach (var childFile in _childFiles.Take(maxTopicCount))
        {
            var relPath = childFile.GetRelativePath(_targetDir);
            var desc = !string.IsNullOrEmpty(childFile.Summary) ? childFile.Summary :
                      !string.IsNullOrEmpty(childFile.Title) ? childFile.Title : "Documentation";
            var displayTitle = !string.IsNullOrEmpty(childFile.Title) ? childFile.Title : relPath;
            lines.Add($"- [{displayTitle}]({relPath}): {desc}");
        }
        
        lines.Add("");
        
        // Common Tasks section (aggregate from all files) - max 8 lines
        var commonTasks = ExtractCommonTasks();
        if (commonTasks.Any())
        {
            lines.Add("## Common Tasks");
            lines.Add("");
            foreach (var link in commonTasks.Take(5))
            {
                lines.Add(link.ToString());
            }
            lines.Add("");
        }
        
        // Reference section - max 7 lines
        var refLinks = ExtractReferenceLinks();
        if (refLinks.Any())
        {
            lines.Add("## Reference");
            lines.Add("");
            foreach (var link in refLinks.Take(3))
            {
                lines.Add(link.ToString());
            }
        }
        
        var content = string.Join("\n", lines);
        var actualLines = content.Trim().Split('\n');
        
        if (actualLines.Length > _maxLines)
        {
            Console.Error.WriteLine($"Warning: Generated content has {actualLines.Length} lines (limit: {_maxLines})");
            Console.Error.WriteLine("Consider reducing the number of items in each section.");
        }
        
        return content;
    }
    
    private LlmsFile? FindFileByPattern(string pattern)
    {
        return _childFiles.FirstOrDefault(f => 
            f.Path.Contains(pattern, StringComparison.OrdinalIgnoreCase));
    }
    
    private List<Link> GetTopLinksFromFile(LlmsFile llmsFile, int limit = 5)
    {
        var allLinks = new List<Link>();
        foreach (var sectionLinks in llmsFile.Sections.Values)
        {
            allLinks.AddRange(sectionLinks);
        }
        return allLinks.Take(limit).ToList();
    }
    
    private List<Link> ExtractCommonTasks()
    {
        var taskSections = new[] { "common tasks", "quick start", "your first apps", "tutorials" };
        var tasks = new List<Link>();
        
        foreach (var child in _childFiles)
        {
            foreach (var (sectionName, links) in child.Sections)
            {
                if (taskSections.Any(ts => sectionName.Contains(ts, StringComparison.OrdinalIgnoreCase)))
                {
                    tasks.AddRange(links);
                }
            }
        }
        
        return tasks;
    }
    
    private List<Link> ExtractReferenceLinks()
    {
        var refSections = new[] { "reference", "api reference", "documentation" };
        var refs = new List<Link>();
        
        foreach (var child in _childFiles)
        {
            foreach (var (sectionName, links) in child.Sections)
            {
                if (refSections.Any(rs => sectionName.Contains(rs, StringComparison.OrdinalIgnoreCase)))
                {
                    refs.AddRange(links);
                }
            }
        }
        
        return refs;
    }
    
    public int ChildFileCount => _childFiles.Count;
}
