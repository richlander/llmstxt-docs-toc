using System.Text.RegularExpressions;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Parser for llms.txt files following the llms.txt format specification.
/// </summary>
public partial class LlmsParser
{
    [GeneratedRegex(@"^-\s+\[([^\]]+)\]\(([^\)]+)\)(?::\s+(.+))?$")]
    private static partial Regex LinkPattern();
    
    [GeneratedRegex(@"^(#{1,3})\s+(.+)$")]
    private static partial Regex HeadingPattern();
    
    [GeneratedRegex(@"^>\s+(.+)$")]
    private static partial Regex BlockquotePattern();
    
    public static LlmsFile ParseFile(string filePath)
    {
        var llmsFile = new LlmsFile { Path = filePath };
        
        if (!File.Exists(filePath))
        {
            return llmsFile;
        }
        
        var lines = File.ReadAllLines(filePath);
        llmsFile.LineCount = lines.Length;
        
        var currentSection = "header";
        
        foreach (var line in lines)
        {
            var trimmedLine = line.TrimEnd();
            
            if (string.IsNullOrWhiteSpace(trimmedLine))
            {
                continue;
            }
            
            // Check for heading
            var headingMatch = HeadingPattern().Match(trimmedLine);
            if (headingMatch.Success)
            {
                var level = headingMatch.Groups[1].Value.Length;
                var title = headingMatch.Groups[2].Value;
                
                if (level == 1 && string.IsNullOrEmpty(llmsFile.Title))
                {
                    llmsFile.Title = title;
                }
                else if (level == 2)
                {
                    currentSection = title;
                    llmsFile.Sections[currentSection] = new List<Link>();
                }
                continue;
            }
            
            // Check for blockquote
            var blockquoteMatch = BlockquotePattern().Match(trimmedLine);
            if (blockquoteMatch.Success)
            {
                var content = blockquoteMatch.Groups[1].Value;
                if (content.StartsWith("Parent:"))
                {
                    llmsFile.ParentLink = content;
                }
                else if (string.IsNullOrEmpty(llmsFile.Summary))
                {
                    llmsFile.Summary = content;
                }
                continue;
            }
            
            // Check for link
            var linkMatch = LinkPattern().Match(trimmedLine);
            if (linkMatch.Success)
            {
                var description = linkMatch.Groups[1].Value;
                var url = linkMatch.Groups[2].Value;
                var extraDesc = linkMatch.Groups[3].Success ? linkMatch.Groups[3].Value : "";
                
                if (!string.IsNullOrEmpty(extraDesc))
                {
                    description = $"{description}: {extraDesc}";
                }
                
                var link = new Link(
                    Url: url,
                    Description: description,
                    SourceFile: filePath,
                    Section: currentSection
                );
                
                if (!llmsFile.Sections.ContainsKey(currentSection))
                {
                    llmsFile.Sections[currentSection] = new List<Link>();
                }
                llmsFile.Sections[currentSection].Add(link);
            }
        }
        
        return llmsFile;
    }
}
