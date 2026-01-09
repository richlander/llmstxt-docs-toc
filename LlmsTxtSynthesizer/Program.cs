using System.CommandLine;
using LlmsTxtSynthesizer;

var targetDirArg = new Argument<string>(
    name: "target-dir",
    description: "Target directory containing toc.yml or llms*.txt files");

var dryRunOption = new Option<bool>(
    aliases: new[] { "--dry-run" },
    description: "Show what would be generated without writing files");

var validateOption = new Option<bool>(
    aliases: new[] { "--validate", "-v" },
    description: "Validate all llms*.txt files (utility mode)");

var treeOption = new Option<bool>(
    aliases: new[] { "--tree", "-t" },
    description: "Show tree structure of all llms*.txt files (utility mode)");

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

var rootCommand = new RootCommand("Generate llms.txt files based on physical file structure from toc.yml references")
{
    targetDirArg,
    dryRunOption,
    validateOption,
    treeOption,
    maxLinesOption,
    titleOption,
    summaryOption
};

rootCommand.SetHandler(async (context) =>
{
    var targetDir = context.ParseResult.GetValueForArgument(targetDirArg);
    var validate = context.ParseResult.GetValueForOption(validateOption);
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var tree = context.ParseResult.GetValueForOption(treeOption);
    var maxLines = context.ParseResult.GetValueForOption(maxLinesOption);
    var title = context.ParseResult.GetValueForOption(titleOption);
    var summary = context.ParseResult.GetValueForOption(summaryOption);
    
    if (!Directory.Exists(targetDir))
    {
        Console.Error.WriteLine($"Error: Directory does not exist: {targetDir}");
        context.ExitCode = 1;
        return;
    }

    // Utility mode: Tree view
    if (tree)
    {
        RecursiveGenerator.ShowTree(targetDir);
        return;
    }

    // Utility mode: Validate
    if (validate)
    {
        var synthesizer = new LlmsSynthesizer(targetDir, maxLines);
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

    // Default mode: Structural generation (files appear where they physically exist)
    var structuralGen = new StructuralLlmsGenerator(maxLines);
    
    try
    {
        var count = structuralGen.GenerateAll(targetDir, dryRun);
        context.ExitCode = 0;
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Error during generation: {ex.Message}");
        context.ExitCode = 1;
    }
    
    await Task.CompletedTask;
});

return await rootCommand.InvokeAsync(args);
