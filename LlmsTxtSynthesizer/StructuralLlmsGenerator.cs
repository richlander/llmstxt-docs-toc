using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Generates llms.txt files based on physical file structure rather than toc.yml presentation.
/// Files appear in llms.txt where they physically exist, not where toc.yml references them.
/// </summary>
public class StructuralLlmsGenerator
{
    private readonly int _maxLines;
    private readonly string _githubBaseUrl;
    private readonly Dictionary<string, FileMetadata> _fileIndex = new();
    private readonly Dictionary<string, List<FileMetadata>> _directorFiles = new();
    private readonly List<(string Dir, int LineCount, int FileCount)> _filesAtLimit = new();

    public StructuralLlmsGenerator(int maxLines = 50, string githubBaseUrl = "https://raw.githubusercontent.com/dotnet/docs/refs/heads/main")
    {
        _maxLines = maxLines;
        _githubBaseUrl = githubBaseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Generate llms.txt files throughout a directory tree based on physical file locations.
    /// </summary>
    public int GenerateAll(string rootDir, bool dryRun = false)
    {
        if (!Directory.Exists(rootDir))
        {
            throw new DirectoryNotFoundException($"Directory not found: {rootDir}");
        }

        Console.WriteLine("Phase 1: Discovering and parsing all toc.yml files...");
        var tocFiles = Directory.GetFiles(rootDir, "toc.yml", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
        
        Console.WriteLine($"Found {tocFiles.Count} toc.yml files\n");

        // Phase 1: Parse all toc.yml files and build global index
        foreach (var tocFile in tocFiles)
        {
            ParseTocFile(tocFile, rootDir);
        }

        Console.WriteLine($"Phase 1 complete: Indexed {_fileIndex.Count} unique files\n");

        // Phase 2: Group files by their physical directory
        Console.WriteLine("Phase 2: Grouping files by physical location...");
        GroupFilesByDirectory(rootDir);
        Console.WriteLine($"Phase 2 complete: {_directorFiles.Count} directories with content\n");

        // Phase 3: Generate llms.txt for each directory
        Console.WriteLine("Phase 3: Generating llms.txt files...\n");
        int generated = 0;
        foreach (var (dir, files) in _directorFiles.OrderBy(kvp => kvp.Key))
        {
            var relPath = Path.GetRelativePath(rootDir, dir);
            Console.WriteLine($"Processing: {relPath}");

            try
            {
                var (outputPath, lineCount) = GenerateLlmsTxt(dir, files, rootDir, dryRun);
                if (outputPath != null)
                {
                    if (dryRun)
                    {
                        Console.WriteLine($"  ⊘ Would create: llms.txt ({files.Count} files)");
                    }
                    else
                    {
                        Console.WriteLine($"  ✓ Created: llms.txt ({lineCount} lines, {files.Count} files)");
                        
                        // Track files at or near limit
                        if (lineCount >= _maxLines)
                        {
                            _filesAtLimit.Add((relPath, lineCount, files.Count));
                        }
                    }
                    generated++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }

        Console.WriteLine($"\nGenerated {generated} llms.txt files");
        
        // Report files at limit
        if (_filesAtLimit.Any())
        {
            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"⚠ Warning: {_filesAtLimit.Count} file(s) hit the {_maxLines}-line limit:\n");
            foreach (var (dir, lineCount, fileCount) in _filesAtLimit.OrderByDescending(f => f.FileCount))
            {
                Console.WriteLine($"  {dir}");
                Console.WriteLine($"    {lineCount} lines, {fileCount} files (some content may be truncated)");
            }
            Console.WriteLine($"\nConsider splitting these directories or increasing --max-lines");
        }
        
        return generated;
    }

    private void ParseTocFile(string tocFilePath, string rootDir)
    {
        var tocDir = Path.GetDirectoryName(tocFilePath)!;
        var yaml = File.ReadAllText(tocFilePath);
        
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
            try
            {
                var items = deserializer.Deserialize<List<TocItem>>(yaml);
                tocRoot = new TocRoot { Items = items };
            }
            catch
            {
                return;
            }
        }

        if (tocRoot?.Items == null || tocRoot.Items.Count == 0)
        {
            return;
        }

        // Extract all file references with their context
        ExtractFileReferences(tocRoot.Items, tocDir, rootDir, null);
    }

    private void ExtractFileReferences(List<TocItem> items, string tocDir, string rootDir, string? category)
    {
        foreach (var item in items)
        {
            // Determine category from parent item if it has children
            var currentCategory = (item.Items != null && item.Items.Count > 0) ? item.Name : category;

            if (!string.IsNullOrEmpty(item.Href) && item.Href.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                var fullPath = Path.GetFullPath(Path.Combine(tocDir, item.Href));
                
                if (File.Exists(fullPath))
                {
                    var relativePath = Path.GetRelativePath(rootDir, fullPath);
                    
                    // Add or update file metadata
                    if (!_fileIndex.ContainsKey(relativePath))
                    {
                        _fileIndex[relativePath] = new FileMetadata
                        {
                            RelativePath = relativePath,
                            FullPath = fullPath,
                            Title = item.Name ?? Path.GetFileNameWithoutExtension(item.Href),
                            Category = currentCategory,
                            Description = ExtractDescriptionFromMarkdown(fullPath)
                        };
                    }
                    else if (currentCategory != null && _fileIndex[relativePath].Category == null)
                    {
                        // Update category if we didn't have one before
                        _fileIndex[relativePath].Category = currentCategory;
                    }
                }
            }

            // Recursively process nested items
            if (item.Items != null && item.Items.Count > 0)
            {
                ExtractFileReferences(item.Items, tocDir, rootDir, currentCategory);
            }
        }
    }

    private void GroupFilesByDirectory(string rootDir)
    {
        foreach (var (_, metadata) in _fileIndex)
        {
            var dir = Path.GetDirectoryName(metadata.FullPath)!;
            
            if (!_directorFiles.ContainsKey(dir))
            {
                _directorFiles[dir] = new List<FileMetadata>();
            }
            
            _directorFiles[dir].Add(metadata);
        }
    }

    private int GenerateParentLlmsTxt(string rootDir, bool dryRun)
    {
        // Find all directories that have child directories with llms.txt
        var directoriesWithLlms = _directorFiles.Keys.ToHashSet();
        var parentDirs = new Dictionary<string, List<string>>(); // parent -> list of child dirs

        foreach (var dir in directoriesWithLlms)
        {
            var parent = Path.GetDirectoryName(dir);
            if (parent != null && Directory.Exists(parent))
            {
                // Check if parent has llms.txt-worthy children
                if (!parentDirs.ContainsKey(parent))
                {
                    parentDirs[parent] = new List<string>();
                }
                parentDirs[parent].Add(dir);
            }
        }

        int generated = 0;
        foreach (var (parentDir, childDirs) in parentDirs.OrderBy(kvp => kvp.Key))
        {
            // Only generate if parent doesn't already have llms.txt from Phase 3
            if (directoriesWithLlms.Contains(parentDir))
            {
                continue; // Already has its own content
            }

            var relPath = Path.GetRelativePath(rootDir, parentDir);
            Console.WriteLine($"Processing: {relPath}");

            var parentDirName = Path.GetFileName(parentDir);
            var title = $"{ConvertToTitleCase(parentDirName)} Docs";
            
            var content = GenerateParentContent(title, childDirs, parentDir, rootDir);
            var outputPath = Path.Combine(parentDir, "llms.txt");
            var lineCount = content.Split('\n').Length;

            if (!dryRun)
            {
                File.WriteAllText(outputPath, content);
                Console.WriteLine($"  ✓ Created: llms.txt ({lineCount} lines, {childDirs.Count} child sections)");
            }
            else
            {
                Console.WriteLine($"  ⊘ Would create: llms.txt ({childDirs.Count} child sections)");
            }

            generated++;
        }

        return generated;
    }

    private string GenerateParentContent(string title, List<string> childDirs, string parentDir, string rootDir)
    {
        var lines = new List<string>
        {
            $"# {title}",
            ""
        };

        // Group child directories by their subdirectory name
        var childLinks = childDirs
            .Select(dir =>
            {
                var childName = Path.GetFileName(dir);
                var relPath = Path.GetRelativePath(parentDir, dir);
                var llmsTxtPath = Path.Combine(relPath, "llms.txt").Replace('\\', '/');
                var gitHubUrl = GetGitHubUrl(Path.Combine(dir, "llms.txt"), rootDir);
                
                return (Title: ConvertToTitleCase(childName), Url: gitHubUrl, Path: relPath);
            })
            .OrderBy(x => x.Title)
            .ToList();

        lines.Add("## Documentation Sections");
        lines.Add("");
        
        foreach (var (childTitle, url, path) in childLinks)
        {
            lines.Add($"- [{childTitle}]({url})");
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private (string?, int) GenerateLlmsTxt(string dir, List<FileMetadata> files, string rootDir, bool dryRun)
    {
        if (files.Count == 0)
        {
            return (null, 0);
        }

        var dirName = Path.GetFileName(dir);
        var title = $"{ConvertToTitleCase(dirName)} Docs";

        // Group files by category
        var sections = files
            .GroupBy(f => f.Category)
            .Select(g => new Section
            {
                Name = g.Key, // null for ungrouped
                Links = g.Select(f => (f.Title, GetGitHubUrl(f.FullPath, rootDir))).ToList()
            })
            .ToList();

        // Put ungrouped items first
        sections = sections.OrderBy(s => s.Name == null ? 0 : 1).ThenBy(s => s.Name).ToList();

        // Check if this directory has child directories with llms.txt
        var childDirs = _directorFiles.Keys
            .Where(childDir => Path.GetDirectoryName(childDir) == dir)
            .OrderBy(childDir => childDir)
            .ToList();

        var content = GenerateContent(title, sections, dir, childDirs, rootDir);
        var outputPath = Path.Combine(dir, "llms.txt");
        var lineCount = content.Split('\n').Length;

        if (!dryRun)
        {
            File.WriteAllText(outputPath, content);
        }

        return (outputPath, lineCount);
    }

    private string GenerateContent(string title, List<Section> sections, string baseDir, List<string> childDirs, string rootDir)
    {
        var lines = new List<string>
        {
            $"# {title}",
            ""
        };

        foreach (var section in sections)
        {
            // Check if we have room for section header (if present) + at least one link
            int headerLines = section.Name != null ? 2 : 0;
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
                if (lines.Count + 1 >= _maxLines)
                {
                    break;
                }

                var description = _fileIndex.Values
                    .FirstOrDefault(f => GetGitHubUrl(f.FullPath, baseDir) == linkUrl)?.Description;

                if (!string.IsNullOrEmpty(description))
                {
                    lines.Add($"- [{linkTitle}]({linkUrl}): {description}");
                }
                else
                {
                    lines.Add($"- [{linkTitle}]({linkUrl})");
                }
            }

            // Add blank line between sections (but not after the last one)
            if (section != sections.Last() && lines.Count + 2 < _maxLines)
            {
                lines.Add("");
            }
        }

        // Add child llms.txt references if any
        if (childDirs.Any() && lines.Count + 3 < _maxLines)
        {
            lines.Add("");
            lines.Add("## Subdirectories");
            lines.Add("");

            foreach (var childDir in childDirs)
            {
                if (lines.Count + 1 >= _maxLines)
                {
                    break;
                }

                var childName = Path.GetFileName(childDir);
                var llmsTxtUrl = GetGitHubUrl(Path.Combine(childDir, "llms.txt"), rootDir);
                lines.Add($"- [{ConvertToTitleCase(childName)}]({llmsTxtUrl})");
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private string GetGitHubUrl(string fullPath, string rootDir)
    {
        var gitRoot = FindGitRoot(rootDir);
        if (gitRoot == null)
        {
            return fullPath;
        }

        var repoRelativePath = Path.GetRelativePath(gitRoot, fullPath);
        return $"{_githubBaseUrl}/{repoRelativePath.Replace('\\', '/')}";
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

    private string ExtractDescriptionFromMarkdown(string filePath)
    {
        try
        {
            var lines = File.ReadLines(filePath).Take(30).ToList();
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
                        break;
                    }
                }

                if (inFrontmatter && line.TrimStart().StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var descLine = line.Substring(line.IndexOf(descriptionPrefix) + descriptionPrefix.Length).Trim();
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
            // Ignore errors
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
}

// Data models specific to structural generation
internal class FileMetadata
{
    public string RelativePath { get; set; } = "";
    public string FullPath { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Category { get; set; }
    public string Description { get; set; } = "";
}
