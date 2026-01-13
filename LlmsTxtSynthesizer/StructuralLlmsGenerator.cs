using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Generates llms.txt files based on physical file structure rather than toc.yml presentation.
/// Files appear in llms.txt where they physically exist, not where toc.yml references them.
/// Supports _llms.json customization files for overrides and structural adjustments.
/// </summary>
public class StructuralLlmsGenerator
{
    private readonly int _softBudget;  // Warning threshold
    private readonly int _hardBudget;  // Overflow trigger for navigation nodes
    private readonly string _githubBaseUrl;
    private readonly Dictionary<string, FileMetadata> _fileIndex = new();
    private readonly Dictionary<string, List<FileMetadata>> _directorFiles = new();
    private readonly List<(string Dir, int LineCount, int FileCount, bool IsOverSoft)> _filesAtLimit = new();
    private Dictionary<string, FileMetadata> _urlToFile = new();  // Reverse lookup by URL
    private CustomizationLoader? _customizations;
    private string? _cachedGitRoot;  // Cached git root to avoid repeated directory traversal
    private bool _gitRootSearched;   // Whether we've already searched for git root
    private IDeserializer? _yamlDeserializer;  // Cached YAML deserializer

    public StructuralLlmsGenerator(int softBudget = 50, int hardBudget = 75, string githubBaseUrl = "https://raw.githubusercontent.com/dotnet/docs/refs/heads/llmstxt")
    {
        _softBudget = softBudget;
        _hardBudget = hardBudget;
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
        
        // Load customizations first
        _customizations = new CustomizationLoader(rootDir);
        _customizations.LoadAll();
        if (_customizations.Count > 0)
        {
            Console.WriteLine($"Found {_customizations.Count} _llms.json customization file(s)");
        }
        
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

        // Build URL reverse lookup for O(1) access
        BuildUrlLookup(rootDir);

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

                        // Track files over soft budget for reporting
                        if (lineCount > _softBudget)
                        {
                            var isOverHard = lineCount > _hardBudget;
                            _filesAtLimit.Add((relPath, lineCount, files.Count, isOverHard));
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

        // Report files over budget thresholds
        if (_filesAtLimit.Any())
        {
            var overHard = _filesAtLimit.Where(f => f.IsOverSoft).ToList();
            var overSoftOnly = _filesAtLimit.Where(f => !f.IsOverSoft).ToList();

            Console.WriteLine($"\n{'=',-60}");

            if (overHard.Any())
            {
                Console.WriteLine($"⚠ Warning: {overHard.Count} file(s) exceeded hard budget ({_hardBudget} lines):\n");
                foreach (var (dir, lineCount, fileCount, _) in overHard.OrderByDescending(f => f.LineCount))
                {
                    Console.WriteLine($"  {dir}");
                    Console.WriteLine($"    {lineCount} lines, {fileCount} files (content truncated)");
                }
            }

            if (overSoftOnly.Any())
            {
                Console.WriteLine($"\nℹ Info: {overSoftOnly.Count} file(s) exceeded soft budget ({_softBudget} lines) but within hard budget:\n");
                foreach (var (dir, lineCount, fileCount, _) in overSoftOnly.OrderByDescending(f => f.LineCount))
                {
                    Console.WriteLine($"  {dir}: {lineCount} lines, {fileCount} files");
                }
            }
        }
        
        return generated;
    }

    private void ParseTocFile(string tocFilePath, string rootDir)
    {
        var tocDir = Path.GetDirectoryName(tocFilePath)!;
        var yaml = File.ReadAllText(tocFilePath);

        // Use cached deserializer to avoid repeated construction
        _yamlDeserializer ??= new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
        var deserializer = _yamlDeserializer;

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
                        var (mdTitle, mdDesc) = ExtractMarkdownMetadata(fullPath);
                        _fileIndex[relativePath] = new FileMetadata
                        {
                            RelativePath = relativePath,
                            NormalizedPath = relativePath.Replace('\\', '/'),
                            FullPath = fullPath,
                            Title = item.Name ?? Path.GetFileNameWithoutExtension(item.Href),
                            MarkdownTitle = mdTitle,
                            Category = currentCategory,
                            Description = mdDesc
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
            
            // Check if this file should be promoted
            var promotedDir = GetPromotedDirectory(metadata.RelativePath, rootDir);
            if (promotedDir != null)
            {
                dir = promotedDir;
            }
            
            if (!_directorFiles.ContainsKey(dir))
            {
                _directorFiles[dir] = new List<FileMetadata>();
            }
            
            _directorFiles[dir].Add(metadata);
        }
    }

    private string? GetPromotedDirectory(string relativePath, string rootDir)
    {
        if (_customizations == null) return null;
        
        // Check all customizations for promote rules that match this file
        var normalizedPath = relativePath.Replace('\\', '/');
        
        // Walk up the directory tree looking for promote rules
        var parts = normalizedPath.Split('/');
        for (int i = 0; i < parts.Length - 1; i++)
        {
            var dirPath = string.Join("/", parts.Take(i + 1));
            var customization = _customizations.GetCustomization(dirPath);
            
            if (customization?.Promote != null)
            {
                foreach (var rule in customization.Promote)
                {
                    var promotePath = rule.Path.Replace('\\', '/');
                    var fullPromotePath = string.IsNullOrEmpty(dirPath) 
                        ? promotePath 
                        : $"{dirPath}/{promotePath}";
                    
                    if (normalizedPath.StartsWith(fullPromotePath, StringComparison.OrdinalIgnoreCase))
                    {
                        // This file matches a promote rule - move it up N levels
                        var fileDir = Path.GetDirectoryName(Path.Combine(rootDir, relativePath))!;
                        var promoted = fileDir;
                        for (int level = 0; level < rule.Levels; level++)
                        {
                            var parent = Path.GetDirectoryName(promoted);
                            if (parent != null)
                            {
                                promoted = parent;
                            }
                        }
                        return promoted;
                    }
                }
            }
        }
        
        return null;
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

        var relativeDir = Path.GetRelativePath(rootDir, dir);
        var normalizedDir = relativeDir == "." ? "" : relativeDir.Replace('\\', '/');

        // Check for customization
        var customization = _customizations?.GetCustomization(normalizedDir);

        // Get title - prefer customization, then index.md frontmatter/H1, then toc.yml name, then dir name
        var dirName = Path.GetFileName(dir);
        var indexFile = files.FirstOrDefault(f =>
            Path.GetFileName(f.RelativePath).Equals("index.md", StringComparison.OrdinalIgnoreCase));
        var tocTitle = indexFile?.Title; // Title from toc.yml

        // Check if toc.yml title is generic (like "Overview", "Index", etc.)
        var genericTitles = new[] { "index", "overview", "introduction", "getting started" };
        var isTocTitleGeneric = string.IsNullOrEmpty(tocTitle) ||
            genericTitles.Any(g => tocTitle.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
            tocTitle.Equals(dirName, StringComparison.OrdinalIgnoreCase);

        // If toc.yml title is generic, use the cached markdown title
        string? indexTitle = null;
        if (isTocTitleGeneric && indexFile != null)
        {
            indexTitle = indexFile.MarkdownTitle;
        }
        else
        {
            indexTitle = tocTitle;
        }

        var title = customization?.Title
            ?? indexTitle
            ?? $"{ConvertToTitleCase(dirName)} Docs";

        // Get description from customization
        var description = customization?.Description;

        // Get preamble from customization (important warnings/cautions)
        var preamble = customization?.Preamble;

        // Get guidance from customization
        var guidance = customization?.Guidance;

        // Filter out excluded files
        var filteredFiles = files
            .Where(f => !(_customizations?.IsFiltered(f.RelativePath) ?? false))
            .ToList();

        // Apply node-level overrides
        foreach (var file in filteredFiles)
        {
            var fileName = Path.GetFileNameWithoutExtension(file.RelativePath);
            var nodeOverride = _customizations?.GetNodeOverride(normalizedDir, fileName);
            if (nodeOverride != null)
            {
                if (!string.IsNullOrEmpty(nodeOverride.Rename))
                    file.Title = nodeOverride.Rename;
                if (!string.IsNullOrEmpty(nodeOverride.Description))
                    file.Description = nodeOverride.Description;
            }
        }

        // Apply offers - if this directory has offers, prioritize those files
        var offers = customization?.Offers ?? new List<string>();
        if (offers.Any())
        {
            // Reorder files: offered files first (in offer order), then others
            var offeredFiles = new List<FileMetadata>();
            var otherFiles = new List<FileMetadata>();

            foreach (var offer in offers)
            {
                var matchingFile = filteredFiles.FirstOrDefault(f =>
                    Path.GetFileNameWithoutExtension(f.RelativePath).Equals(offer, StringComparison.OrdinalIgnoreCase));
                if (matchingFile != null)
                {
                    offeredFiles.Add(matchingFile);
                }
            }

            otherFiles = filteredFiles.Where(f => !offeredFiles.Contains(f)).ToList();
            filteredFiles = offeredFiles.Concat(otherFiles).ToList();
        }

        // Check for custom sections from customization
        var customSections = customization?.Sections ?? new List<SectionDefinition>();
        List<Section> sections;

        if (customSections.Any())
        {
            // Use custom section definitions
            sections = BuildCustomSections(customSections, filteredFiles, rootDir, normalizedDir);
        }
        else
        {
            // Group files by category (default behavior)
            var rawSections = filteredFiles
                .GroupBy(f => f.Category)
                .Select(g => new Section
                {
                    Name = g.Key, // null for ungrouped
                    Links = g.Select(f => (f.Title, GetGitHubUrl(f.FullPath, rootDir))).ToList()
                })
                .ToList();

            // Merge all "top-level" sections: null category OR category matches title (would have no header)
            var titleWithoutDocs = title.Replace(" Docs", "");
            var topLevelLinks = rawSections
                .Where(s => s.Name == null || s.Name.Equals(titleWithoutDocs, StringComparison.OrdinalIgnoreCase))
                .SelectMany(s => s.Links)
                .ToList();

            var namedSections = rawSections
                .Where(s => s.Name != null && !s.Name.Equals(titleWithoutDocs, StringComparison.OrdinalIgnoreCase))
                .OrderBy(s => s.Name)
                .ToList();

            // Build final section list: top-level first (no header), then named sections
            sections = new List<Section>();
            if (topLevelLinks.Any())
            {
                sections.Add(new Section { Name = null, Links = topLevelLinks });
            }
            sections.AddRange(namedSections);
        }

        // Check if this directory has child directories with llms.txt
        var childDirs = _directorFiles.Keys
            .Where(childDir => Path.GetDirectoryName(childDir) == dir)
            .OrderBy(childDir => childDir)
            .ToList();

        // Get related topics from customization
        var related = customization?.Related ?? new List<RelatedTopic>();

        // Estimate total line count to check for overflow
        var estimatedLines = EstimateLineCount(title, description, guidance, sections, childDirs, related);

        // Determine effective budget:
        // - Navigation nodes (have children): use hard budget, overflow if exceeded
        // - Leaf nodes (no children): use 2x hard budget since overflow would be pointless
        var isLeafDirectory = !childDirs.Any();
        var effectiveBudget = isLeafDirectory ? _hardBudget * 2 : _hardBudget;

        // Check overflow condition: exceeds hard budget AND has more than 1 local file AND has children
        // (no point creating overflow for leaf directories - just use larger budget)
        var needsOverflow = estimatedLines > _hardBudget && filteredFiles.Count > 1 && childDirs.Any();
        string? overflowPath = null;

        if (needsOverflow)
        {
            // Generate overflow file with local content (2x hard budget)
            var overflowFileName = $"llms-{dirName}.txt";
            overflowPath = Path.Combine(dir, overflowFileName);
            var overflowTitle = $"{ConvertToTitleCase(dirName)} Overview";

            var overflowContent = GenerateOverflowContent(overflowTitle, description, sections, rootDir, _hardBudget * 2);
            var overflowLineCount = overflowContent.Split('\n').Length;

            if (!dryRun)
            {
                File.WriteAllText(overflowPath, overflowContent);
            }

            Console.WriteLine($"    → Overflow: {overflowFileName} ({overflowLineCount} lines, {filteredFiles.Count} files)");

            // Clear sections for main file - local content goes in overflow
            sections = new List<Section>();
        }

        // Generate main llms.txt (with or without local content depending on overflow)
        var content = GenerateContent(title, description, preamble, guidance, sections, dir, childDirs, rootDir, related, filteredFiles, customization?.Sections, overflowPath, dirName, effectiveBudget);
        var outputPath = Path.Combine(dir, "llms.txt");
        var lineCount = content.Split('\n').Length;

        if (!dryRun)
        {
            File.WriteAllText(outputPath, content);
        }

        return (outputPath, lineCount);
    }

    private int EstimateLineCount(string title, string? description, GuidanceSection? guidance,
        List<Section> sections, List<string> childDirs, List<RelatedTopic> related)
    {
        int lines = 2; // Title + blank

        if (!string.IsNullOrEmpty(description))
            lines += 2; // Description + blank

        if (guidance?.Items?.Any() == true)
            lines += 3 + guidance.Items.Count; // Header + blank + intro? + items + blank

        foreach (var section in sections)
        {
            if (section.Name != null)
                lines += 2; // Section header + blank
            if (!string.IsNullOrEmpty(section.Description))
                lines += 2;
            lines += section.Links.Count;
            lines += 1; // Blank after section
        }

        if (childDirs.Any())
            lines += 3 + childDirs.Count; // Header + blank + children

        if (related.Any())
            lines += 3 + related.Count;

        return lines;
    }

    private string GenerateOverflowContent(string title, string? description, List<Section> sections, string rootDir, int maxLines)
    {
        var lines = new List<string>
        {
            $"# {title}",
            ""
        };

        if (!string.IsNullOrEmpty(description))
        {
            lines.Add($"> {description}");
            lines.Add("");
        }

        foreach (var section in sections)
        {
            if (lines.Count + 2 >= maxLines)
                break;

            if (section.Name != null)
            {
                lines.Add($"## {section.Name}");
                lines.Add("");
            }

            if (!string.IsNullOrEmpty(section.Description))
            {
                lines.Add(section.Description);
                lines.Add("");
            }

            foreach (var (linkTitle, linkUrl) in section.Links)
            {
                if (lines.Count + 1 >= maxLines)
                    break;

                var fileDesc = GetFileByUrl(linkUrl)?.Description;

                if (!string.IsNullOrEmpty(fileDesc))
                {
                    lines.Add($"- [{linkTitle}]({linkUrl}): {fileDesc}");
                }
                else
                {
                    lines.Add($"- [{linkTitle}]({linkUrl})");
                }
            }

            if (section != sections.Last() && lines.Count + 2 < maxLines)
            {
                lines.Add("");
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private List<Section> BuildCustomSections(List<SectionDefinition> customSections, 
        List<FileMetadata> localFiles, string rootDir, string currentDir)
    {
        var sections = new List<(Section Section, int Priority)>();
        
        foreach (var sectionDef in customSections)
        {
            var priority = sectionDef.Priority ?? 0;
            var links = new List<(string Title, string Url)>();
            
            if (sectionDef.Path != null)
            {
                // This section references a child directory - link to its llms.txt
                // Build full relative path by combining currentDir with the path from section def
                var childDirPath = string.IsNullOrEmpty(currentDir)
                    ? sectionDef.Path
                    : $"{currentDir}/{sectionDef.Path}";

                var childCustomization = _customizations?.GetCustomization(childDirPath);
                var sectionTitle = childCustomization?.Title ?? sectionDef.Name ?? ConvertToTitleCase(sectionDef.Path);
                var sectionDesc = childCustomization?.Description;

                // Get child's offers and parent's wants
                var childOffers = childCustomization?.Offers ?? new List<string>();
                var wants = sectionDef.Wants ?? new List<string>();

                // Resolve what files to include
                var toInclude = new List<string>();
                if (wants.Any())
                {
                    toInclude.AddRange(wants);
                    foreach (var offer in childOffers)
                    {
                        if (toInclude.Count >= 6) break;
                        if (!toInclude.Contains(offer, StringComparer.OrdinalIgnoreCase))
                            toInclude.Add(offer);
                    }
                }
                else if (childOffers.Any())
                {
                    toInclude.AddRange(childOffers.Take(6));
                }

                // Find files from the global index matching this child path
                var childFiles = _fileIndex.Values
                    .Where(f => f.NormalizedPath.StartsWith(childDirPath + "/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (toInclude.Any())
                {
                    // Add specific files in order
                    foreach (var include in toInclude)
                    {
                        var match = childFiles.FirstOrDefault(f =>
                            Path.GetFileNameWithoutExtension(f.RelativePath).Equals(include, StringComparison.OrdinalIgnoreCase));
                        if (match != null)
                        {
                            links.Add((match.Title, GetGitHubUrl(match.FullPath, rootDir)));
                        }
                    }
                }

                // Always add link to the child's llms.txt as the index
                var childLlmsFullPath = Path.Combine(rootDir, childDirPath, "llms.txt");
                var llmsUrl = GetGitHubUrl(childLlmsFullPath, rootDir);
                var indexTitle = $"{sectionTitle} Documentation Index";
                links.Add((indexTitle, llmsUrl));

                var section = new Section
                {
                    Name = sectionTitle,
                    Description = sectionDesc,
                    Links = links
                };
                sections.Add((section, priority));
            }
            else if (sectionDef.Include != null && sectionDef.Include.Any())
            {
                // Explicit include list - can be files or directories
                foreach (var includePath in sectionDef.Include)
                {
                    // First check if it's a file in the global index
                    var matchingFile = _fileIndex.Values.FirstOrDefault(f =>
                        f.NormalizedPath.Equals(includePath + ".md", StringComparison.OrdinalIgnoreCase) ||
                        f.NormalizedPath.EndsWith("/" + includePath + ".md", StringComparison.OrdinalIgnoreCase) ||
                        f.NormalizedPath.EndsWith("/" + includePath + "/overview.md", StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingFile != null)
                    {
                        links.Add((matchingFile.Title, GetGitHubUrl(matchingFile.FullPath, rootDir)));
                    }
                    else
                    {
                        // Assume it's a directory - link to its llms.txt
                        var dirFullPath = Path.Combine(rootDir, includePath, "llms.txt");
                        var dirCustomization = _customizations?.GetCustomization(includePath);
                        var dirTitle = dirCustomization?.Title ?? ConvertToTitleCase(Path.GetFileName(includePath));
                        links.Add((dirTitle, GetGitHubUrl(dirFullPath, rootDir)));
                    }
                }
                
                var section = new Section
                {
                    Name = sectionDef.Name,
                    Links = links
                };
                
                if (links.Any())
                {
                    sections.Add((section, priority));
                }
            }
        }
        
        // Sort by priority (higher first), then by name
        return sections
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.Section.Name)
            .Select(s => s.Section)
            .ToList();
    }

    private string GenerateContent(string title, string? description, string? preamble, GuidanceSection? guidance,
        List<Section> sections, string baseDir, List<string> childDirs, string rootDir,
        List<RelatedTopic> related, List<FileMetadata> files, List<SectionDefinition>? sectionDefs,
        string? overflowPath = null, string? dirName = null, int? budget = null)
    {
        var lines = new List<string>
        {
            $"# {title}",
            ""
        };

        // Add description as blockquote if present
        if (!string.IsNullOrEmpty(description))
        {
            lines.Add($"> {description}");
            lines.Add("");
        }

        // Add preamble (important warnings/cautions) if present
        if (!string.IsNullOrEmpty(preamble))
        {
            lines.Add(preamble);
            lines.Add("");
        }

        // Add guidance section if present
        if (guidance != null && guidance.Items?.Any() == true)
        {
            var guidanceTitle = guidance.Title ?? "Guidance for AI Assistants";
            lines.Add($"## {guidanceTitle}");
            lines.Add("");
            
            if (!string.IsNullOrEmpty(guidance.Intro))
            {
                lines.Add(guidance.Intro);
                lines.Add("");
            }

            foreach (var item in guidance.Items)
            {
                lines.Add($"- {item}");
            }
            lines.Add("");
        }

        foreach (var section in sections)
        {
            // Skip section header if it's redundant with document title (case-insensitive)
            var sectionMatchesTitle = section.Name != null &&
                section.Name.Equals(title.Replace(" Docs", ""), StringComparison.OrdinalIgnoreCase);

            // Only add section header if section has a name and doesn't match the title
            if (section.Name != null && !sectionMatchesTitle)
            {
                lines.Add($"## {section.Name}");
                lines.Add("");
            }

            // Add section description if present
            if (!string.IsNullOrEmpty(section.Description))
            {
                lines.Add(section.Description);
                lines.Add("");
            }

            foreach (var (linkTitle, linkUrl) in section.Links)
            {
                // Try to find description from file index
                var fileDesc = GetFileByUrl(linkUrl)?.Description;

                if (!string.IsNullOrEmpty(fileDesc))
                {
                    lines.Add($"- [{linkTitle}]({linkUrl}): {fileDesc}");
                }
                else
                {
                    lines.Add($"- [{linkTitle}]({linkUrl})");
                }
            }

            // Add blank line between sections (but not after the last one)
            if (section != sections.Last())
            {
                lines.Add("");
            }
        }

        // Add overflow file as its own section if it exists
        if (overflowPath != null && dirName != null)
        {
            if (lines.Count > 0 && !string.IsNullOrEmpty(lines[^1]))
            {
                lines.Add("");
            }
            var overflowUrl = GetGitHubUrl(overflowPath, rootDir);
            var overflowDisplayName = $"{ConvertToTitleCase(dirName)} Overview";
            lines.Add($"## {overflowDisplayName}");
            lines.Add("");
            lines.Add($"- [Documentation files in this directory]({overflowUrl})");
        }

        // Add child directories as embedded sections with their offered content
        if (childDirs.Any())
        {
            // Build priority map and skip list from section definitions
            var priorityMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var coveredBySection = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (sectionDefs != null)
            {
                foreach (var secDef in sectionDefs)
                {
                    if (secDef.Path != null)
                    {
                        // If this path has a section definition, it's already rendered by BuildCustomSections
                        coveredBySection.Add(secDef.Path);
                        if (secDef.Priority.HasValue)
                        {
                            priorityMap[secDef.Path] = secDef.Priority.Value;
                        }
                    }
                }
            }

            // Order children: by priority (higher first), then alphabetically
            // Skip children that are already covered by custom sections
            var orderedChildren = childDirs
                .Select(childDir => {
                    var childName = Path.GetFileName(childDir);
                    var priority = priorityMap.TryGetValue(childName, out var p) ? p : 0;
                    return (Dir: childDir, Name: childName, Priority: priority);
                })
                .Where(c => !coveredBySection.Contains(c.Name)) // Skip already-rendered children
                .OrderByDescending(c => c.Priority)
                .ThenBy(c => c.Name)
                .ToList();

            foreach (var (childDir, childName, _) in orderedChildren)
            {
                // Get child's customization - use path relative to rootDir (same as CustomizationLoader uses)
                var childRelPath = Path.GetRelativePath(rootDir, childDir).Replace('\\', '/');
                var childCustomization = _customizations?.GetCustomization(childRelPath);

                var displayName = childCustomization?.Title ?? ConvertToTitleCase(childName);
                var llmsTxtUrl = GetGitHubUrl(Path.Combine(childDir, "llms.txt"), rootDir);

                // Add section header for this child
                if (lines.Count > 0 && !string.IsNullOrEmpty(lines[^1]))
                {
                    lines.Add("");
                }
                lines.Add($"## {displayName}");
                lines.Add("");

                // Get child's offers and embed them (supports transitive offers from subdirectories)
                var childOffers = childCustomization?.Offers ?? new List<string>();
                var offersAdded = 0;

                foreach (var offer in childOffers)
                {
                    // Check if offer is a subdirectory (transitive offer)
                    var subDirPath = Path.Combine(childDir, offer);
                    if (_directorFiles.ContainsKey(subDirPath))
                    {
                        // It's a subdirectory - get its offers and embed them
                        var subDirRelPath = Path.GetRelativePath(rootDir, subDirPath).Replace('\\', '/');
                        var subDirCustomization = _customizations?.GetCustomization(subDirRelPath);
                        var subDirOffers = subDirCustomization?.Offers ?? new List<string>();

                        foreach (var subOffer in subDirOffers)
                        {
                            var subMatchingFile = _fileIndex.Values.FirstOrDefault(f =>
                                f.NormalizedPath.StartsWith(subDirRelPath + "/", StringComparison.OrdinalIgnoreCase) &&
                                Path.GetFileNameWithoutExtension(f.RelativePath).Equals(subOffer, StringComparison.OrdinalIgnoreCase));

                            if (subMatchingFile != null)
                            {
                                var fileUrl = GetGitHubUrl(subMatchingFile.FullPath, rootDir);
                                if (!string.IsNullOrEmpty(subMatchingFile.Description))
                                {
                                    lines.Add($"- [{subMatchingFile.Title}]({fileUrl}): {subMatchingFile.Description}");
                                }
                                else
                                {
                                    lines.Add($"- [{subMatchingFile.Title}]({fileUrl})");
                                }
                                offersAdded++;
                            }
                        }
                    }
                    else
                    {
                        // It's a file - find in the global index
                        var matchingFile = _fileIndex.Values.FirstOrDefault(f =>
                            f.NormalizedPath.StartsWith(childRelPath + "/", StringComparison.OrdinalIgnoreCase) &&
                            Path.GetFileNameWithoutExtension(f.RelativePath).Equals(offer, StringComparison.OrdinalIgnoreCase));

                        if (matchingFile != null)
                        {
                            var fileUrl = GetGitHubUrl(matchingFile.FullPath, rootDir);
                            if (!string.IsNullOrEmpty(matchingFile.Description))
                            {
                                lines.Add($"- [{matchingFile.Title}]({fileUrl}): {matchingFile.Description}");
                            }
                            else
                            {
                                lines.Add($"- [{matchingFile.Title}]({fileUrl})");
                            }
                            offersAdded++;
                        }
                    }
                }

                // Always add link to full index
                {
                    if (offersAdded > 0)
                    {
                        lines.Add($"- [More in {displayName}...]({llmsTxtUrl})");
                    }
                    else
                    {
                        // No offers - just link to the full index with description if available
                        var childDesc = childCustomization?.Description;
                        if (!string.IsNullOrEmpty(childDesc))
                        {
                            lines.Add($"- [{displayName} Index]({llmsTxtUrl}): {childDesc}");
                        }
                        else
                        {
                            lines.Add($"- [{displayName} Index]({llmsTxtUrl})");
                        }
                    }
                }
            }
        }

        // Add related topics if any
        if (related.Any())
        {
            lines.Add("");
            lines.Add("## Related Topics");
            lines.Add("");

            foreach (var topic in related.OrderByDescending(r => r.Weight ?? 0))
            {

                var reason = topic.Reason ?? string.Join(", ", topic.Keywords ?? new List<string>());
                if (!string.IsNullOrEmpty(reason))
                {
                    lines.Add($"- [{topic.Path}]({topic.Path}): {reason}");
                }
                else
                {
                    lines.Add($"- [{topic.Path}]({topic.Path})");
                }
            }
        }

        return string.Join("\n", lines).TrimEnd() + "\n";
    }

    private void BuildUrlLookup(string rootDir)
    {
        _urlToFile = new Dictionary<string, FileMetadata>(_fileIndex.Count);
        foreach (var file in _fileIndex.Values)
        {
            var url = GetGitHubUrl(file.FullPath, rootDir);
            _urlToFile[url] = file;
        }
    }

    private FileMetadata? GetFileByUrl(string url)
    {
        return _urlToFile.TryGetValue(url, out var file) ? file : null;
    }

    private string GetGitHubUrl(string fullPath, string rootDir)
    {
        var gitRoot = GetCachedGitRoot(rootDir);
        if (gitRoot == null)
        {
            return fullPath;
        }

        var repoRelativePath = Path.GetRelativePath(gitRoot, fullPath);
        return $"{_githubBaseUrl}/{repoRelativePath.Replace('\\', '/')}";
    }

    private string? GetCachedGitRoot(string startPath)
    {
        if (_gitRootSearched)
        {
            return _cachedGitRoot;
        }

        _gitRootSearched = true;
        var currentDir = new DirectoryInfo(startPath);
        while (currentDir != null)
        {
            if (Directory.Exists(Path.Combine(currentDir.FullName, ".git")))
            {
                _cachedGitRoot = currentDir.FullName;
                return _cachedGitRoot;
            }
            currentDir = currentDir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Extract both title and description from markdown frontmatter in a single file read.
    /// </summary>
    private (string? Title, string Description) ExtractMarkdownMetadata(string filePath)
    {
        string? title = null;
        string description = "";

        try
        {
            var lines = File.ReadLines(filePath).Take(30).ToList();
            var inFrontmatter = false;
            var titlePrefix = "title:";
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

                if (inFrontmatter)
                {
                    // Check for title
                    if (line.TrimStart().StartsWith(titlePrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var titleLine = line.Substring(line.IndexOf(titlePrefix) + titlePrefix.Length).Trim();
                        // Remove quotes if present
                        if (titleLine.StartsWith("\"") && titleLine.EndsWith("\""))
                        {
                            titleLine = titleLine.Substring(1, titleLine.Length - 2);
                        }
                        // Remove common suffixes like " - .NET" or " | Microsoft Learn"
                        var dashIndex = titleLine.IndexOf(" - ");
                        if (dashIndex > 0)
                        {
                            titleLine = titleLine.Substring(0, dashIndex);
                        }
                        var pipeIndex = titleLine.IndexOf(" | ");
                        if (pipeIndex > 0)
                        {
                            titleLine = titleLine.Substring(0, pipeIndex);
                        }
                        title = titleLine;
                    }
                    // Check for description
                    else if (line.TrimStart().StartsWith(descriptionPrefix, StringComparison.OrdinalIgnoreCase))
                    {
                        var descLine = line.Substring(line.IndexOf(descriptionPrefix) + descriptionPrefix.Length).Trim();
                        if (descLine.StartsWith("\"") && descLine.EndsWith("\""))
                        {
                            descLine = descLine.Substring(1, descLine.Length - 2);
                        }
                        description = descLine;
                    }
                }

                // Also check for H1 heading as fallback for title (after frontmatter)
                if (!inFrontmatter && line.StartsWith("# ") && title == null)
                {
                    title = line.Substring(2).Trim();
                }
            }
        }
        catch
        {
            // Ignore errors
        }

        return (title, description);
    }

    private string ConvertToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "Documentation";
        }

        // Special cases for known acronyms and terms
        var specialCases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ai", "AI" },
            { "iot", "IoT" },
            { "csharp", "C#" },
            { "fsharp", "F#" },
            { "vb", "VB" },
            { "aspnet", "ASP.NET" },
            { "ml", "ML" },
            { "api", "API" },
            { "sdk", "SDK" },
            { "cli", "CLI" },
            { "devops", "DevOps" },
            { "wcf", "WCF" },
            { "wpf", "WPF" },
            { "ef", "EF" }
        };

        var words = input.Split('-', '_', ' ')
            .Where(w => !string.IsNullOrEmpty(w))
            .Select(w =>
            {
                // Check if it's a special case
                if (specialCases.TryGetValue(w, out var special))
                {
                    return special;
                }
                // Otherwise normal title case
                return char.ToUpper(w[0]) + w.Substring(1).ToLower();
            });

        return string.Join(" ", words);
    }
}

// Data models specific to structural generation
internal class FileMetadata
{
    public string RelativePath { get; set; } = "";
    public string NormalizedPath { get; set; } = "";  // Path with forward slashes, computed once
    public string FullPath { get; set; } = "";
    public string Title { get; set; } = "";           // Title from toc.yml
    public string? MarkdownTitle { get; set; }        // Title extracted from markdown frontmatter
    public string? Category { get; set; }
    public string Description { get; set; } = "";
}
