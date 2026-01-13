namespace LlmsTxtSynthesizer;

/// <summary>
/// Generates llms.txt files based on physical file structure.
/// Discovers markdown files directly and extracts metadata from frontmatter.
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
    private string? _customizationRoot;  // Root directory for customization path resolution

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

        Console.WriteLine("Phase 1: Discovering markdown files...");

        // Load customizations first
        _customizations = new CustomizationLoader(rootDir);
        _customizations.LoadAll();
        _customizationRoot = rootDir;
        if (_customizations.Count > 0)
        {
            Console.WriteLine($"Found {_customizations.Count} _llms.json customization file(s)");
        }

        // Phase 1: Discover all markdown files and build global index
        DiscoverMarkdownFiles(rootDir);

        Console.WriteLine($"Phase 1 complete: Indexed {_fileIndex.Count} markdown files\n");

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

        // Phase 4: Generate navigation-only llms.txt for intermediate directories
        // (directories with no content but with children that have llms.txt)
        var intermediateGenerated = GenerateIntermediateDirectories(rootDir, dryRun);
        if (intermediateGenerated > 0)
        {
            Console.WriteLine($"Generated {intermediateGenerated} navigation-only llms.txt files");
            generated += intermediateGenerated;
        }

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

    /// <summary>
    /// Discover all markdown files in the directory tree and build the file index.
    /// </summary>
    private void DiscoverMarkdownFiles(string rootDir)
    {
        var mdFiles = Directory.GetFiles(rootDir, "*.md", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        foreach (var mdFile in mdFiles)
        {
            var relativePath = Path.GetRelativePath(rootDir, mdFile);

            // Skip files in common non-documentation directories
            if (relativePath.Contains("node_modules") ||
                relativePath.Contains(".git") ||
                relativePath.Contains("bin/") ||
                relativePath.Contains("obj/"))
            {
                continue;
            }

            var (title, description) = ExtractMarkdownMetadata(mdFile);
            var fileName = Path.GetFileNameWithoutExtension(mdFile);

            _fileIndex[relativePath] = new FileMetadata
            {
                RelativePath = relativePath,
                NormalizedPath = relativePath.Replace('\\', '/'),
                FullPath = mdFile,
                Title = title ?? ConvertToTitleCase(fileName),
                MarkdownTitle = title,
                Category = null, // No category without toc.yml hierarchy
                Description = description
            };
        }
    }

    /// <summary>
    /// Generate navigation-only llms.txt for intermediate directories.
    /// These are directories that have no content themselves but contain children with llms.txt.
    /// </summary>
    private int GenerateIntermediateDirectories(string rootDir, bool dryRun)
    {
        // Find all directories that have llms.txt (from Phase 3)
        var directoriesWithContent = _directorFiles.Keys.ToHashSet();

        // Find the common root of all content directories
        // But don't go above the git root
        var gitRoot = GetCachedGitRoot(rootDir);
        var effectiveRoot = FindCommonRoot(directoriesWithContent, rootDir);
        if (gitRoot != null && effectiveRoot.Length < gitRoot.Length)
        {
            effectiveRoot = gitRoot;
        }

        // Reload customizations from effective root if it differs from rootDir
        // This ensures we pick up _llms.json files from intermediate directories
        if (effectiveRoot != rootDir && _customizations != null)
        {
            _customizations = new CustomizationLoader(effectiveRoot);
            _customizations.LoadAll();
        }

        // Find intermediate directories: directories that are ancestors of content directories
        // but don't have content themselves
        var intermediateDirectories = new Dictionary<string, List<string>>(); // intermediate dir -> child dirs with content

        foreach (var contentDir in directoriesWithContent)
        {
            // Walk up from each content directory, looking for gaps
            var current = Path.GetDirectoryName(contentDir);
            var child = contentDir;

            while (current != null && current.Length > effectiveRoot.Length && current.StartsWith(effectiveRoot))
            {
                if (directoriesWithContent.Contains(current))
                {
                    // Hit a directory with content, stop walking up
                    break;
                }

                // This is an intermediate directory (has no content)
                if (!intermediateDirectories.ContainsKey(current))
                {
                    intermediateDirectories[current] = new List<string>();
                }

                // Add the child (either direct content dir or another intermediate that we'll process)
                // Only add direct children, not grandchildren
                var directChild = child;
                if (!intermediateDirectories[current].Contains(directChild))
                {
                    intermediateDirectories[current].Add(directChild);
                }

                child = current;
                current = Path.GetDirectoryName(current);
            }
        }

        if (!intermediateDirectories.Any())
        {
            return 0;
        }

        Console.WriteLine($"\nPhase 4: Generating navigation-only files for {intermediateDirectories.Count} intermediate directories...\n");

        int generated = 0;

        // Process from deepest to shallowest so children are available when processing parents
        foreach (var (dir, children) in intermediateDirectories.OrderByDescending(kvp => kvp.Key.Length))
        {
            var relPath = Path.GetRelativePath(rootDir, dir);
            Console.WriteLine($"Processing: {relPath} (navigation-only)");

            try
            {
                var (outputPath, lineCount) = GenerateNavigationOnlyLlmsTxt(dir, children, rootDir, effectiveRoot, dryRun);
                if (outputPath != null)
                {
                    // Add to _directorFiles so parent directories can see it
                    _directorFiles[dir] = new List<FileMetadata>(); // Empty content, but registered

                    if (dryRun)
                    {
                        Console.WriteLine($"  ⊘ Would create: llms.txt (navigation, {children.Count} children)");
                    }
                    else
                    {
                        Console.WriteLine($"  ✓ Created: llms.txt ({lineCount} lines, navigation to {children.Count} children)");
                    }
                    generated++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }
        }

        return generated;
    }

    private (string?, int) GenerateNavigationOnlyLlmsTxt(string dir, List<string> childDirs, string rootDir, string customizationRoot, bool dryRun)
    {
        var dirName = Path.GetFileName(dir);

        // Use customizationRoot for looking up customizations
        var customizationPath = Path.GetRelativePath(customizationRoot, dir);
        var normalizedCustomizationPath = customizationPath == "." ? "" : customizationPath.Replace('\\', '/');

        // Check for customization
        var customization = _customizations?.GetCustomization(normalizedCustomizationPath);

        // Get title and description from customization or defaults
        var title = customization?.Title ?? $"{ConvertToTitleCase(dirName)}";
        var description = customization?.Description;

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

        // Add each child directory as a section
        foreach (var childDir in childDirs.OrderBy(d => d))
        {
            var childName = Path.GetFileName(childDir);
            var childCustomizationPath = Path.GetRelativePath(customizationRoot, childDir).Replace('\\', '/');
            var childCustomization = _customizations?.GetCustomization(childCustomizationPath);

            var childTitle = childCustomization?.Title ?? ConvertToTitleCase(childName);
            var childDesc = childCustomization?.Description;
            var llmsTxtUrl = GetGitHubUrl(Path.Combine(childDir, "llms.txt"), rootDir);

            lines.Add($"## {childTitle}");
            lines.Add("");

            if (!string.IsNullOrEmpty(childDesc))
            {
                lines.Add(childDesc);
                lines.Add("");
            }

            // Get child's offers and embed them
            var childOffers = childCustomization?.Offers ?? new List<string>();
            foreach (var offer in childOffers.Take(4)) // Limit offers for navigation files
            {
                var matchingFile = _fileIndex.Values.FirstOrDefault(f =>
                    f.NormalizedPath.StartsWith(childCustomizationPath + "/", StringComparison.OrdinalIgnoreCase) &&
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
                }
            }

            // Add link to full index
            if (childOffers.Any())
            {
                lines.Add($"- [More in {childTitle}...]({llmsTxtUrl})");
            }
            else
            {
                lines.Add($"- [{childTitle} Documentation Index]({llmsTxtUrl})");
            }

            lines.Add("");
        }

        var content = string.Join("\n", lines).TrimEnd() + "\n";
        var outputPath = Path.Combine(dir, "llms.txt");
        var lineCount = content.Split('\n').Length;

        if (!dryRun)
        {
            File.WriteAllText(outputPath, content);
        }

        return (outputPath, lineCount);
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

    private (string?, int) GenerateLlmsTxt(string dir, List<FileMetadata> files, string rootDir, bool dryRun)
    {
        if (files.Count == 0)
        {
            return (null, 0);
        }

        // Use customizationRoot for path resolution
        var customizationPath = Path.GetRelativePath(_customizationRoot ?? rootDir, dir);
        var normalizedCustomizationPath = customizationPath == "." ? "" : customizationPath.Replace('\\', '/');

        // Check for customization
        var customization = _customizations?.GetCustomization(normalizedCustomizationPath);

        // Get title - prefer customization, then index.md frontmatter/H1, then dir name
        var dirName = Path.GetFileName(dir);
        var indexFile = files.FirstOrDefault(f =>
            Path.GetFileName(f.RelativePath).Equals("index.md", StringComparison.OrdinalIgnoreCase));

        // Check if index title is generic (like "Overview", "Index", etc.)
        var genericTitles = new[] { "index", "overview", "introduction", "getting started" };
        var indexTitle = indexFile?.MarkdownTitle;
        var isIndexTitleGeneric = string.IsNullOrEmpty(indexTitle) ||
            genericTitles.Any(g => indexTitle.Equals(g, StringComparison.OrdinalIgnoreCase)) ||
            indexTitle.Equals(dirName, StringComparison.OrdinalIgnoreCase);

        // If index title is generic, fall back to directory name
        string? effectiveTitle = null;
        if (!isIndexTitleGeneric && indexFile != null)
        {
            effectiveTitle = indexFile.MarkdownTitle;
        }

        var title = customization?.Title
            ?? effectiveTitle
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
            var nodeOverride = _customizations?.GetNodeOverride(normalizedCustomizationPath, fileName);
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
            // Use custom section definitions (for child directories)
            sections = BuildCustomSections(customSections, filteredFiles, rootDir, normalizedCustomizationPath);
        }
        else
        {
            sections = new List<Section>();
        }

        // Always include local files as an ungrouped section (even when custom sections exist)
        // Custom sections are for organizing child directories - local files are separate
        if (filteredFiles.Any())
        {
            var localSection = new Section
            {
                Name = null,
                Links = filteredFiles.Select(f => (f.Title, GetGitHubUrl(f.FullPath, rootDir))).ToList()
            };
            // Insert local files at the beginning
            sections.Insert(0, localSection);
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
            // Generate extended index file with additional content (not custom sections)
            // Custom sections (prioritized child content) stay in main file
            var additionalSection = sections.FirstOrDefault(s => s.Name == null);
            if (additionalSection != null)
            {
                var overflowFileName = "llms-extended.txt";
                overflowPath = Path.Combine(dir, overflowFileName);
                var overflowTitle = $"{title} -- Extended Index";

                var overflowSections = new List<Section> { additionalSection };
                var overflowContent = GenerateOverflowContent(overflowTitle, description, overflowSections, rootDir, _hardBudget * 2);
                var overflowLineCount = overflowContent.Split('\n').Length;

                if (!dryRun)
                {
                    File.WriteAllText(overflowPath, overflowContent);
                }

                Console.WriteLine($"    → Extended: {overflowFileName} ({overflowLineCount} lines, {filteredFiles.Count} additional topics)");

                // Remove additional content from main sections - it's now in extended index
                // Keep custom sections (prioritized content) in main file
                sections = sections.Where(s => s.Name != null).ToList();
            }
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

                // Calculate how many offers to include based on priority (100 = 100%, 50 = 50%, etc.)
                // Always round up so that 50% of 1 offer still shows 1 offer
                var offerCount = childOffers.Count;
                var offersToInclude = priority > 0 && offerCount > 0
                    ? (int)Math.Ceiling(offerCount * priority / 100.0)
                    : 0;

                // Resolve what files to include
                var toInclude = new List<string>();
                if (wants.Any())
                {
                    toInclude.AddRange(wants);
                    foreach (var offer in childOffers)
                    {
                        if (toInclude.Count >= offersToInclude) break;
                        if (!toInclude.Contains(offer, StringComparer.OrdinalIgnoreCase))
                            toInclude.Add(offer);
                    }
                }
                else if (childOffers.Any() && offersToInclude > 0)
                {
                    toInclude.AddRange(childOffers.Take(offersToInclude));
                }

                // Find files from the global index matching this child path
                var childFiles = _fileIndex.Values
                    .Where(f => f.NormalizedPath.StartsWith(childDirPath + "/", StringComparison.OrdinalIgnoreCase))
                    .ToList();

                if (toInclude.Any())
                {
                    // Add specific files or subdirectory links in order
                    foreach (var include in toInclude)
                    {
                        // First check if it's a subdirectory
                        var subDirPath = $"{childDirPath}/{include}";
                        var subDirFullPath = Path.Combine(rootDir, subDirPath);
                        if (_directorFiles.ContainsKey(subDirFullPath) || Directory.Exists(subDirFullPath))
                        {
                            // It's a subdirectory - link to its llms.txt
                            var subDirCustomization = _customizations?.GetCustomization(subDirPath);
                            var subDirTitle = subDirCustomization?.Title ?? ConvertToTitleCase(include);
                            var subDirLlmsUrl = GetGitHubUrl(Path.Combine(subDirFullPath, "llms.txt"), rootDir);
                            links.Add((subDirTitle, subDirLlmsUrl));
                        }
                        else
                        {
                            // It's a file - find in the index
                            var match = childFiles.FirstOrDefault(f =>
                                Path.GetFileNameWithoutExtension(f.RelativePath).Equals(include, StringComparison.OrdinalIgnoreCase));
                            if (match != null)
                            {
                                links.Add((match.Title, GetGitHubUrl(match.FullPath, rootDir)));
                            }
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
            var topicCount = files.Count;
            lines.Add("## More Topics");
            lines.Add("");
            lines.Add($"- [{topicCount} more topics]({overflowUrl})");
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

            foreach (var (childDir, childName, childPriority) in orderedChildren)
            {
                // Get child's customization - use path relative to customizationRoot
                var childCustomizationPath = Path.GetRelativePath(_customizationRoot ?? rootDir, childDir).Replace('\\', '/');
                var childCustomization = _customizations?.GetCustomization(childCustomizationPath);

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

                // Calculate how many offers to include based on priority (100 = 100%, 50 = 50%, etc.)
                // Always round up so that 50% of 1 offer still shows 1 offer
                var offerCount = childOffers.Count;
                var maxOffersToInclude = childPriority > 0 && offerCount > 0
                    ? (int)Math.Ceiling(offerCount * childPriority / 100.0)
                    : offerCount; // If no priority set, include all offers

                foreach (var offer in childOffers)
                {
                    // Check if we've reached the limit based on priority
                    if (offersAdded >= maxOffersToInclude)
                        break;

                    // Check if offer is a subdirectory (transitive offer)
                    var subDirPath = Path.Combine(childDir, offer);
                    if (_directorFiles.ContainsKey(subDirPath))
                    {
                        // It's a subdirectory - get its offers and embed them
                        var subDirCustomizationPath = Path.GetRelativePath(_customizationRoot ?? rootDir, subDirPath).Replace('\\', '/');
                        var subDirCustomization = _customizations?.GetCustomization(subDirCustomizationPath);
                        var subDirOffers = subDirCustomization?.Offers ?? new List<string>();

                        foreach (var subOffer in subDirOffers)
                        {
                            var subMatchingFile = _fileIndex.Values.FirstOrDefault(f =>
                                f.NormalizedPath.StartsWith(subDirCustomizationPath + "/", StringComparison.OrdinalIgnoreCase) &&
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
                            f.NormalizedPath.StartsWith(childCustomizationPath + "/", StringComparison.OrdinalIgnoreCase) &&
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

    private string FindCommonRoot(HashSet<string> directories, string fallback)
    {
        if (!directories.Any())
        {
            return fallback;
        }

        // Start with the first directory and find common prefix with all others
        var common = directories.First();

        foreach (var dir in directories.Skip(1))
        {
            // Find common prefix
            var minLen = Math.Min(common.Length, dir.Length);
            var i = 0;
            while (i < minLen && common[i] == dir[i])
            {
                i++;
            }

            common = common.Substring(0, i);
        }

        // If the common prefix is already a valid directory, use it
        if (Directory.Exists(common))
        {
            return common;
        }

        // Otherwise, trim to last directory separator
        // (the common prefix might end mid-directory-name)
        var lastSep = common.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSep > 0)
        {
            common = common.Substring(0, lastSep);
        }

        // Ensure we have a valid directory path
        if (string.IsNullOrEmpty(common) || !Directory.Exists(common))
        {
            return fallback;
        }

        return common;
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
    public string Title { get; set; } = "";           // Title from markdown frontmatter
    public string? MarkdownTitle { get; set; }        // Title extracted from markdown frontmatter
    public string? Category { get; set; }
    public string Description { get; set; } = "";
}
