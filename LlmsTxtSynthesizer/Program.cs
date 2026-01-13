using System.CommandLine;
using LlmsTxtSynthesizer;

var targetDirArg = new Argument<string>(
    name: "target-dir",
    description: "Target directory containing markdown files to generate llms.txt from");

var dryRunOption = new Option<bool>(
    aliases: new[] { "--dry-run" },
    description: "Show what would be generated without writing files");

var validateOption = new Option<bool>(
    aliases: new[] { "--validate", "-v" },
    description: "Validate all llms*.txt files (utility mode)");

var treeOption = new Option<bool>(
    aliases: new[] { "--tree", "-t" },
    description: "Show tree structure of all llms*.txt files (utility mode)");

var softBudgetOption = new Option<int>(
    aliases: new[] { "--soft-budget" },
    getDefaultValue: () => 50,
    description: "Soft budget threshold for warnings (default: 50)");

var hardBudgetOption = new Option<int>(
    aliases: new[] { "--hard-budget" },
    getDefaultValue: () => 75,
    description: "Hard budget threshold that triggers overflow (default: 75)");

var titleOption = new Option<string>(
    aliases: new[] { "--title" },
    getDefaultValue: () => ".NET Documentation",
    description: "Title for root llms.txt");

var summaryOption = new Option<string>(
    aliases: new[] { "--summary" },
    getDefaultValue: () => "Build applications for any platform with C#, F#, and Visual Basic.",
    description: "Summary for root llms.txt");

var rootCommand = new RootCommand("Generate llms.txt files based on physical file structure from markdown files")
{
    targetDirArg,
    dryRunOption,
    validateOption,
    treeOption,
    softBudgetOption,
    hardBudgetOption,
    titleOption,
    summaryOption
};

rootCommand.SetHandler(async (context) =>
{
    var targetDir = context.ParseResult.GetValueForArgument(targetDirArg);
    var validate = context.ParseResult.GetValueForOption(validateOption);
    var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
    var tree = context.ParseResult.GetValueForOption(treeOption);
    var softBudget = context.ParseResult.GetValueForOption(softBudgetOption);
    var hardBudget = context.ParseResult.GetValueForOption(hardBudgetOption);
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
        ShowTree(targetDir);
        return;
    }

    // Utility mode: Validate
    if (validate)
    {
        var synthesizer = new LlmsSynthesizer(targetDir, hardBudget);
        var childFiles = synthesizer.DiscoverChildFiles();
        var allValid = true;

        foreach (var path in childFiles)
        {
            var (isValid, issues) = Validator.ValidateFile(path, hardBudget);
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
    var structuralGen = new StructuralLlmsGenerator(softBudget, hardBudget);
    
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

static void ShowTree(string targetDir)
{
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
