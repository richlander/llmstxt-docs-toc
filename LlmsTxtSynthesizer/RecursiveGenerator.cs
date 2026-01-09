namespace LlmsTxtSynthesizer;

/// <summary>
/// Performs recursive depth-first generation of llms.txt files throughout a directory tree.
/// Generates llms.txt files from the deepest directories first, then works up to the root.
/// </summary>
public class RecursiveGenerator
{
    private readonly int _maxLines;
    private readonly string _title;
    private readonly string _summary;
    private int _filesGenerated;

    public RecursiveGenerator(int maxLines = 50, string? title = null, string? summary = null)
    {
        _maxLines = maxLines;
        _title = title ?? ".NET Documentation";
        _summary = summary ?? "Build applications for any platform with C#, F#, and Visual Basic.";
    }

    public int FilesGenerated => _filesGenerated;

    /// <summary>
    /// Converts toc.yml files to llms.txt throughout a directory tree.
    /// Returns the path to the root llms.txt file.
    /// </summary>
    public string GenerateRecursive(string targetDir, bool dryRun = false)
    {
        _filesGenerated = 0;
        
        if (!Directory.Exists(targetDir))
        {
            throw new DirectoryNotFoundException($"Directory not found: {targetDir}");
        }

        // Convert all toc.yml files to llms.txt
        Console.WriteLine("Converting toc.yml files to llms.txt\n");
        Console.WriteLine($"{'=',-60}");
        var converter = new YmlToLlmsConverter(_maxLines);
        var convertedCount = converter.ConvertAllInDirectory(targetDir, dryRun);
        Console.WriteLine($"{'=',-60}\n");
        
        _filesGenerated = convertedCount;
        
        var rootPath = Path.Combine(targetDir, "llms.txt");
        
        Console.WriteLine($"{'=',-60}");
        Console.WriteLine($"Summary:");
        Console.WriteLine($"  Total llms.txt files generated: {_filesGenerated}");
        if (!dryRun && File.Exists(rootPath))
        {
            Console.WriteLine($"  Root file: {rootPath}");
        }
        else if (dryRun)
        {
            Console.WriteLine($"  Mode: Dry run (no files written)");
        }
        Console.WriteLine($"{'=',-60}");

        return rootPath;
    }

    /// <summary>
    /// Find all directories that contain llms*.txt files in subdirectories.
    /// These are directories that need a synthesized llms.txt file.
    /// Directories with only llms-*.txt in themselves (no subdirs) don't need synthesis.
    /// </summary>
    private HashSet<string> FindDirectoriesWithLlmsFiles(string targetDir)
    {
        var dirs = new HashSet<string>();

        foreach (var file in Directory.EnumerateFiles(targetDir, "llms*.txt", SearchOption.AllDirectories))
        {
            var fileName = Path.GetFileName(file);
            var fileDir = Path.GetDirectoryName(file)!;
            
            // Skip llms.txt files (we're looking for llms-*.txt source files)
            if (fileName == "llms.txt")
            {
                continue;
            }
            
            // Only synthesize llms.txt if there are child files in subdirectories
            // Don't synthesize for leaf directories (those with llms-*.txt but no subdirs with llms files)
            var parentDir = Path.GetDirectoryName(fileDir);
            if (!string.IsNullOrEmpty(parentDir) && parentDir != targetDir && parentDir.StartsWith(targetDir))
            {
                // This file is in a subdirectory - add its parent to the synthesis list
                dirs.Add(parentDir);
            }
        }

        return dirs;
    }

    /// <summary>
    /// Get the depth of a directory relative to the target directory.
    /// </summary>
    private int GetDepth(string dir, string targetDir)
    {
        var relPath = Path.GetRelativePath(targetDir, dir);
        if (relPath == ".")
        {
            return 0;
        }
        return relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
    }

    /// <summary>
    /// Generate a title for a directory based on its name.
    /// </summary>
    private string GetDirectoryTitle(string dir, string targetDir)
    {
        if (dir == targetDir)
        {
            return _title;
        }

        var dirName = Path.GetFileName(dir);
        
        // Convert directory name to title case
        // e.g., "getting-started" -> "Getting Started"
        if (string.IsNullOrEmpty(dirName))
        {
            return "Documentation";
        }

        var words = dirName.Split('-', '_', ' ')
            .Select(w => w.Length > 0 ? char.ToUpper(w[0]) + w.Substring(1).ToLower() : w);
        
        return string.Join(" ", words);
    }

    /// <summary>
    /// Try to get a summary for a directory from existing llms files.
    /// </summary>
    private string GetDirectorySummary(string dir)
    {
        // Look for an index or readme type file in this directory
        var llmsFiles = Directory.GetFiles(dir, "llms*.txt")
            .Where(f => Path.GetFileName(f) != "llms.txt")
            .ToList();

        if (llmsFiles.Count > 0)
        {
            // Parse the first file and try to get its summary
            var parsed = LlmsParser.ParseFile(llmsFiles[0]);
            if (!string.IsNullOrEmpty(parsed.Summary))
            {
                return parsed.Summary;
            }
        }

        // Default summary
        return $"Documentation for {Path.GetFileName(dir)}";
    }

    /// <summary>
    /// Discover all llms*.txt files and show the tree structure.
    /// </summary>
    public static void ShowTree(string targetDir)
    {
        if (!Directory.Exists(targetDir))
        {
            Console.WriteLine($"Directory not found: {targetDir}");
            return;
        }

        Console.WriteLine($"Directory tree for: {targetDir}\n");

        var allFiles = Directory.GetFiles(targetDir, "llms*.txt", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();

        if (allFiles.Count == 0)
        {
            Console.WriteLine("No llms*.txt files found.");
            return;
        }

        var dirGroups = allFiles.GroupBy(f => Path.GetDirectoryName(f)!);

        foreach (var group in dirGroups)
        {
            var dir = group.Key;
            var relPath = Path.GetRelativePath(targetDir, dir);
            var depth = relPath == "." ? 0 : relPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length;
            var indent = new string(' ', depth * 2);

            Console.WriteLine($"{indent}{(relPath == "." ? "." : Path.GetFileName(dir))}/");

            foreach (var file in group.OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                var fileSize = new FileInfo(file).Length;
                var lineCount = File.ReadAllLines(file).Length;
                
                Console.WriteLine($"{indent}  - {fileName} ({lineCount} lines, {fileSize} bytes)");
            }
        }

        Console.WriteLine($"\nTotal: {allFiles.Count} files in {dirGroups.Count()} directories");
    }
}
