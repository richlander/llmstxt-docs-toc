using System.Text.RegularExpressions;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Validates llms.txt files against constraints.
/// </summary>
public partial class Validator
{
    [GeneratedRegex(@"^-\s+\[([^\]]+)\]\(([^\)]+)\)")]
    private static partial Regex LinkPattern();
    
    public static (bool IsValid, List<string> Issues) ValidateFile(string filePath, int maxLines = 50)
    {
        var issues = new List<string>();
        
        if (!File.Exists(filePath))
        {
            return (false, new List<string> { $"File does not exist: {filePath}" });
        }
        
        var lines = File.ReadAllLines(filePath);
        var lineCount = lines.Length;
        
        if (lineCount > maxLines)
        {
            issues.Add($"File exceeds {maxLines} line limit: {lineCount} lines");
        }
        
        // Check for broken link format
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.StartsWith('-') && line.Contains('['))
            {
                if (!LinkPattern().IsMatch(line))
                {
                    issues.Add($"Line {i + 1}: Malformed link format");
                }
            }
        }
        
        return (issues.Count == 0, issues);
    }
}
