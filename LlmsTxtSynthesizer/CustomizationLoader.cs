using System.Text.Json;

namespace LlmsTxtSynthesizer;

/// <summary>
/// Loads and manages _llms.json customization files from a directory tree.
/// Builds a hierarchy where child customizations can be referenced by parents.
/// </summary>
public class CustomizationLoader
{
    private readonly Dictionary<string, LlmsCustomization> _customizations = new();
    private readonly string _rootDir;

    public CustomizationLoader(string rootDir)
    {
        _rootDir = Path.GetFullPath(rootDir);
    }

    /// <summary>
    /// Discover and load all _llms.json files in the directory tree.
    /// </summary>
    public void LoadAll()
    {
        var files = Directory.GetFiles(_rootDir, "_llms.json", SearchOption.AllDirectories);
        
        foreach (var file in files)
        {
            try
            {
                var json = File.ReadAllText(file);
                var customization = JsonSerializer.Deserialize<LlmsCustomization>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (customization != null)
                {
                    var dir = Path.GetDirectoryName(file)!;
                    var relativeDir = Path.GetRelativePath(_rootDir, dir);
                    
                    // Normalize to forward slashes and handle root
                    var key = relativeDir == "." ? "" : relativeDir.Replace('\\', '/');
                    _customizations[key] = customization;
                }
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Warning: Failed to parse {Path.GetRelativePath(_rootDir, file)}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Get customization for a specific directory path (relative to root).
    /// </summary>
    public LlmsCustomization? GetCustomization(string relativePath)
    {
        var key = relativePath.Replace('\\', '/');
        return _customizations.TryGetValue(key, out var c) ? c : null;
    }

    /// <summary>
    /// Get customization for root directory.
    /// </summary>
    public LlmsCustomization? GetRootCustomization()
    {
        return _customizations.TryGetValue("", out var c) ? c : null;
    }

    /// <summary>
    /// Check if a path should be filtered out.
    /// </summary>
    public bool IsFiltered(string relativePath)
    {
        // Check each ancestor's filter list
        var parts = relativePath.Replace('\\', '/').Split('/');
        var current = "";

        for (int i = 0; i < parts.Length; i++)
        {
            if (_customizations.TryGetValue(current, out var customization))
            {
                if (customization.Filter != null)
                {
                    // Build the remaining path from current position
                    var remainingPath = string.Join("/", parts.Skip(i));
                    if (customization.Filter.Any(f => 
                        remainingPath.Equals(f, StringComparison.OrdinalIgnoreCase) ||
                        remainingPath.StartsWith(f + "/", StringComparison.OrdinalIgnoreCase)))
                    {
                        return true;
                    }
                }
            }

            current = string.IsNullOrEmpty(current) ? parts[i] : $"{current}/{parts[i]}";
        }

        return false;
    }

    /// <summary>
    /// Get the effective title for a path, checking for overrides.
    /// </summary>
    public string? GetTitleOverride(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Title;
    }

    /// <summary>
    /// Get the effective description for a path, checking for overrides.
    /// </summary>
    public string? GetDescriptionOverride(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Description;
    }

    /// <summary>
    /// Get node-level override for a specific file within a directory.
    /// </summary>
    public NodeOverride? GetNodeOverride(string dirPath, string nodeName)
    {
        var customization = GetCustomization(dirPath);
        if (customization?.Nodes == null)
            return null;

        return customization.Nodes.TryGetValue(nodeName, out var nodeOverride) ? nodeOverride : null;
    }

    /// <summary>
    /// Get the offers list from a child, used by parent to decide what to include.
    /// </summary>
    public List<string> GetOffers(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Offers ?? new List<string>();
    }

    /// <summary>
    /// Get sections defined for a path.
    /// </summary>
    public List<SectionDefinition> GetSections(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Sections ?? new List<SectionDefinition>();
    }

    /// <summary>
    /// Get guidance for a path.
    /// </summary>
    public GuidanceSection? GetGuidance(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Guidance;
    }

    /// <summary>
    /// Get related topics for a path.
    /// </summary>
    public List<RelatedTopic> GetRelated(string relativePath)
    {
        var customization = GetCustomization(relativePath);
        return customization?.Related ?? new List<RelatedTopic>();
    }

    /// <summary>
    /// Resolve what nodes should be included from a child based on parent wants and child offers.
    /// </summary>
    public List<string> ResolveIncludes(string parentPath, string childPath, int budget = 6)
    {
        var parentCustomization = GetCustomization(parentPath);
        var childCustomization = GetCustomization(childPath);

        // Find the section in parent that references this child
        var section = parentCustomization?.Sections?
            .FirstOrDefault(s => s.Path?.Equals(childPath, StringComparison.OrdinalIgnoreCase) == true);

        var wants = section?.Wants ?? new List<string>();
        var offers = childCustomization?.Offers ?? new List<string>();

        if (wants.Any())
        {
            // Parent specified what they want - use that, then fill with offers
            var result = new List<string>(wants);
            foreach (var offer in offers)
            {
                if (result.Count >= budget) break;
                if (!result.Contains(offer, StringComparer.OrdinalIgnoreCase))
                {
                    result.Add(offer);
                }
            }
            return result.Take(budget).ToList();
        }
        else if (offers.Any())
        {
            // No wants specified, use offers up to budget
            return offers.Take(budget).ToList();
        }

        // No customization - return empty (caller will include all)
        return new List<string>();
    }

    public int Count => _customizations.Count;
}
