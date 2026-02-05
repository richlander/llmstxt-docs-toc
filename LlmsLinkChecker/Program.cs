using System.CommandLine;
using LlmsLinkChecker;

var rootFileArg = new Argument<string>(
    name: "llms-txt-path",
    description: "Path to the root llms.txt file to check");

var verboseOption = new Option<bool>(
    aliases: ["--verbose", "-v"],
    description: "Show verbose output including each URL being checked");

var recursiveOption = new Option<bool>(
    aliases: ["--recursive", "-r"],
    getDefaultValue: () => true,
    description: "Recursively follow and check linked llms.txt files");

var timeoutOption = new Option<int>(
    aliases: ["--timeout", "-t"],
    getDefaultValue: () => 30,
    description: "Timeout in seconds for each HTTP request");

var concurrencyOption = new Option<int>(
    aliases: ["--concurrency", "-c"],
    getDefaultValue: () => 10,
    description: "Maximum number of concurrent HTTP requests");

var rootCommand = new RootCommand("Check links in llms.txt files for 404s and other HTTP errors")
{
    rootFileArg,
    verboseOption,
    recursiveOption,
    timeoutOption,
    concurrencyOption
};

rootCommand.SetHandler(async (context) =>
{
    var rootFile = context.ParseResult.GetValueForArgument(rootFileArg);
    var verbose = context.ParseResult.GetValueForOption(verboseOption);
    var recursive = context.ParseResult.GetValueForOption(recursiveOption);
    var timeout = context.ParseResult.GetValueForOption(timeoutOption);
    var concurrency = context.ParseResult.GetValueForOption(concurrencyOption);
    var cancellationToken = context.GetCancellationToken();

    // Check if the root file exists
    if (!File.Exists(rootFile))
    {
        Console.Error.WriteLine($"Error: File does not exist: {rootFile}");
        context.ExitCode = 1;
        return;
    }

    Console.WriteLine($"Checking links in: {rootFile}");
    Console.WriteLine($"Recursive: {recursive}, Timeout: {timeout}s, Concurrency: {concurrency}");
    Console.WriteLine();

    var checkedUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var failedLinks = new List<LinkCheckResult>();
    var totalLinksChecked = 0;
    var llmsTxtFilesProcessed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var pendingLlmsTxtUrls = new Queue<string>();

    using var checker = new LinkChecker(verbose, concurrency, timeout);

    // Start with the root file (local)
    var rootContent = await File.ReadAllTextAsync(rootFile, cancellationToken);
    await ProcessLlmsTxtContent(rootContent, rootFile);

    // Process any discovered llms.txt URLs recursively
    while (recursive && pendingLlmsTxtUrls.Count > 0)
    {
        var llmsTxtUrl = pendingLlmsTxtUrls.Dequeue();
        
        if (llmsTxtFilesProcessed.Contains(llmsTxtUrl))
            continue;
            
        llmsTxtFilesProcessed.Add(llmsTxtUrl);
        
        Console.WriteLine($"\nFollowing llms.txt: {llmsTxtUrl}");
        
        var (success, content, error) = await checker.FetchContentAsync(llmsTxtUrl, cancellationToken);
        if (success && content != null)
        {
            await ProcessLlmsTxtContent(content, llmsTxtUrl);
        }
        else
        {
            Console.WriteLine($"  ✗ Failed to fetch: {error}");
            failedLinks.Add(new LinkCheckResult(
                new ExtractedLink(llmsTxtUrl, "llms.txt", "root", 0),
                0, false, error));
        }
    }

    // Print summary
    Console.WriteLine();
    Console.WriteLine("═══════════════════════════════════════════════════════════════");
    Console.WriteLine($"Summary: Checked {totalLinksChecked} links");
    Console.WriteLine($"         llms.txt files processed: {llmsTxtFilesProcessed.Count + 1}");
    
    if (failedLinks.Count == 0)
    {
        Console.WriteLine("         ✓ All links returned 2xx status codes");
        context.ExitCode = 0;
    }
    else
    {
        Console.WriteLine($"         ✗ {failedLinks.Count} links with non-2xx status:");
        Console.WriteLine();
        
        foreach (var result in failedLinks.OrderBy(r => r.StatusCode == 0 ? 999 : r.StatusCode))
        {
            var status = result.StatusCode > 0 ? $"HTTP {result.StatusCode}" : "ERROR";
            Console.WriteLine($"  [{status}] {result.Link.Url}");
            Console.WriteLine($"           Source: {result.Link.SourceFile}:{result.Link.LineNumber}");
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"           Reason: {result.ErrorMessage}");
            }
            Console.WriteLine();
        }
        
        context.ExitCode = 1;
    }

    async Task ProcessLlmsTxtContent(string content, string sourceFile)
    {
        var links = LinkExtractor.ExtractLinks(content, sourceFile).ToList();
        var linksToCheck = new List<ExtractedLink>();
        
        foreach (var link in links)
        {
            // Skip already checked URLs
            if (checkedUrls.Contains(link.Url))
                continue;
                
            checkedUrls.Add(link.Url);
            
            // Queue llms.txt files for recursive processing
            if (recursive && LinkExtractor.IsLlmsTxtLink(link.Url) && !llmsTxtFilesProcessed.Contains(link.Url))
            {
                pendingLlmsTxtUrls.Enqueue(link.Url);
            }
            
            linksToCheck.Add(link);
        }

        if (linksToCheck.Count == 0)
        {
            Console.WriteLine($"  No new links to check in {Path.GetFileName(sourceFile)}");
            return;
        }

        Console.WriteLine($"  Checking {linksToCheck.Count} links from {Path.GetFileName(sourceFile)}...");

        // Check all links concurrently
        var tasks = linksToCheck.Select(link => checker.CheckLinkAsync(link, cancellationToken));
        var results = await Task.WhenAll(tasks);

        totalLinksChecked += results.Length;

        // Report and collect failures
        foreach (var result in results.Where(r => !r.IsSuccess))
        {
            failedLinks.Add(result);
            var status = result.StatusCode > 0 ? $"HTTP {result.StatusCode}" : "ERROR";
            Console.WriteLine($"    ✗ [{status}] {result.Link.Url}");
            if (verbose && !string.IsNullOrEmpty(result.ErrorMessage))
            {
                Console.WriteLine($"      Reason: {result.ErrorMessage}");
            }
        }

        var successCount = results.Count(r => r.IsSuccess);
        if (successCount > 0 && !verbose)
        {
            Console.WriteLine($"    ✓ {successCount} links OK");
        }
    }
});

return await rootCommand.InvokeAsync(args);
