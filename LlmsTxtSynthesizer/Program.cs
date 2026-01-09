using System.CommandLine;
using LlmsTxtSynthesizer;

var targetDirArg = new Argument<string>(
    name: "target-dir",
    description: "Target directory containing llms*.txt files");

var generateOption = new Option<bool>(
    aliases: new[] { "--generate", "-g" },
    description: "Generate root llms.txt file");

var validateOption = new Option<bool>(
    aliases: new[] { "--validate", "-v" },
    description: "Validate all llms*.txt files");

var discoverOption = new Option<bool>(
    aliases: new[] { "--discover", "-d" },
    description: "Discover and list child llms*.txt files");

var recursiveOption = new Option<bool>(
    aliases: new[] { "--recursive", "-r" },
    description: "Recursively generate llms.txt files depth-first");

var dryRunOption = new Option<bool>(
    aliases: new[] { "--dry-run" },
    description: "Show what would be generated without writing files");

var treeOption = new Option<bool>(
    aliases: new[] { "--tree", "-t" },
    description: "Show tree structure of all llms*.txt files");

var outputOption = new Option<string?>(
    aliases: new[] { "--output", "-o" },
    description: "Output file for generated content (default: stdout)");

var maxLinesOption = new Option<int>(
    aliases: new[] { "--max-lines" },
    getDefaultValue: () => 50,
    description: "Maximum lines per file (default: 50)");

var titleOption = new Option<string>(
    aliases: new[] { "--title" },
    getDefaultValue: () => ".NET Documentation",
    description: "Title for root llms.txt");

var summaryOption = new Option<string>(
    aliases: new[] { "--summary" },
    getDefaultValue: () => "Build applications for any platform with C#, F#, and Visual Basic.",
    description: "Summary for root llms.txt");

var rootCommand = new RootCommand("Synthesize root llms.txt from child files")
{
    targetDirArg,
    generateOption,
    validateOption,
    discoverOption,
    recursiveOption,
    dryRunOption,
    treeOption,
    outputOption,
    maxLinesOption,
    titleOption,
    summaryOption
};

rootCommand.SetHandler(async (context) =>
{
    var targetDir = context.ParseResult.GetValueForArgument(targetDirArg);
    var generate = context.ParseResult.GetValueForOption(generateOption);
    var validate = context.ParseResult.GetValueForOption(validateOption);
    var discover = context.ParseResult.GetValueForOption(discoverOption);
    var recursive = context.ParseResult.GetValueForOption(recursiveOption);
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var tree = context.ParseResult.GetValueForOption(treeOption);
    var output = context.ParseResult.GetValueForOption(outputOption);
    var maxLines = context.ParseResult.GetValueForOption(maxLinesOption);
    var title = context.ParseResult.GetValueForOption(titleOption);
    var summary = context.ParseResult.GetValueForOption(summaryOption);
    
    if (!Directory.Exists(targetDir))
    {
        Console.Error.WriteLine($"Error: Directory does not exist: {targetDir}");
        context.ExitCode = 1;
        return;
    }

    var synthesizer = new LlmsSynthesizer(targetDir, maxLines);

    // Tree mode
    if (tree)
    {
        RecursiveGenerator.ShowTree(targetDir);
        return;
    }

    // Recursive mode
    if (recursive)
    {
        var generator = new RecursiveGenerator(maxLines, title, summary);
        
        try
        {
            var rootPath = generator.GenerateRecursive(targetDir, dryRun);
            
            Console.WriteLine($"\n{'=',-60}");
            Console.WriteLine($"Summary:");
            Console.WriteLine($"  Total files generated: {generator.FilesGenerated}");
            if (!dryRun)
            {
                Console.WriteLine($"  Root file: {rootPath}");
            }
            else
            {
                Console.WriteLine($"  Mode: Dry run (no files written)");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error during recursive generation: {ex.Message}");
            context.ExitCode = 1;
        }
        
        return;
    }

    // Discover mode
    if (discover)
    {
        var childFiles = synthesizer.DiscoverChildFiles();
        if (childFiles.Count > 0)
        {
            Console.WriteLine($"Found {childFiles.Count} child llms*.txt files:");
            foreach (var path in childFiles)
            {
                var relPath = Path.GetRelativePath(targetDir, path);
                Console.WriteLine($"  - {relPath}");
            }
        }
        else
        {
            Console.WriteLine("No child llms*.txt files found.");
        }
        return;
    }

    // Validate mode
    if (validate)
    {
        var childFiles = synthesizer.DiscoverChildFiles();
        var allValid = true;

        foreach (var path in childFiles)
        {
            var (isValid, issues) = Validator.ValidateFile(path, maxLines);
            if (!isValid)
            {
                allValid = false;
                var relPath = Path.GetRelativePath(targetDir, path);
                Console.WriteLine($"\n{relPath}:");
                foreach (var issue in issues)
                {
                    Console.WriteLine($"  ✗ {issue}");
                }
            }
            else
            {
                var relPath = Path.GetRelativePath(targetDir, path);
                var lineCount = File.ReadAllLines(path).Length;
                Console.WriteLine($"✓ {relPath} ({lineCount} lines)");
            }
        }

        context.ExitCode = allValid ? 0 : 1;
        return;
    }

    // Generate mode
    if (generate)
    {
        synthesizer.LoadChildFiles();
        var content = synthesizer.GenerateRootContent(title, summary);

        if (!string.IsNullOrEmpty(output))
        {
            await File.WriteAllTextAsync(output, content);
            Console.WriteLine($"Generated: {output}");

            var lineCount = content.Trim().Split('\n').Length;
            Console.WriteLine($"Lines: {lineCount}/{maxLines}");
            Console.WriteLine($"Child files processed: {synthesizer.ChildFileCount}");
        }
        else
        {
            Console.WriteLine(content);
        }

        return;
    }

    // No mode specified
    Console.WriteLine("Please specify a mode: --generate, --validate, --discover, --recursive, or --tree");
    Console.WriteLine("Use --help for more information.");
    context.ExitCode = 1;
});

return await rootCommand.InvokeAsync(args);
