using System.Text.RegularExpressions;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Service for parsing and applying gitignore rules
/// </summary>
public static class GitignoreService
{
    // Cache of parsed gitignore rules per directory to avoid re-parsing
    private static readonly Dictionary<string, List<GitignoreRule>> _gitignoreCache = new();
    
    /// <summary>
    /// Loads the .gitignore rules for a given directory
    /// </summary>
    /// <param name="directory">The directory to load gitignore rules for</param>
    /// <returns>A list of gitignore rules. Returns an empty list if no rules are found or an error occurs.</returns>
    public static List<GitignoreRule> LoadGitignoreRules(string directory)
    {
        string normalizedDir = Path.GetFullPath(directory);
        
        if (_gitignoreCache.TryGetValue(normalizedDir, out var cachedRules))
        {
            return cachedRules ?? new List<GitignoreRule>(); // Ensure non-null return
        }
        
        var rules = new List<GitignoreRule>();
        string? currentDir = normalizedDir; // Allow currentDir to be null
        
        while (!string.IsNullOrEmpty(currentDir))
        {
            string gitignorePath = Path.Combine(currentDir, ".gitignore");
            
            if (File.Exists(gitignorePath))
            {
                try
                {
                    var gitignoreFileRules = ParseGitignore(gitignorePath, currentDir);
                    rules.AddRange(gitignoreFileRules);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing .gitignore at {gitignorePath}: {ex.Message}");
                }
            }
            
            string? parentDir = Path.GetDirectoryName(currentDir);
            if (parentDir == currentDir || parentDir == null) // Break if root or parent is null
                break;
                
            currentDir = parentDir;
        }
        
        _gitignoreCache[normalizedDir] = rules;
        return rules;
    }
    
    /// <summary>
    /// Parses a .gitignore file and returns a list of rules
    /// </summary>
    /// <param name="gitignorePath">Path to the .gitignore file</param>
    /// <param name="basePath">Base path where the .gitignore file is located</param>
    /// <returns>List of GitignoreRule objects. Returns an empty list if an error occurs.</returns>
    private static List<GitignoreRule> ParseGitignore(string gitignorePath, string basePath)
    {
        var rules = new List<GitignoreRule>();
        try
        {
            var lines = File.ReadAllLines(gitignorePath);
            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;
                
                bool negated = trimmedLine.StartsWith("!");
                string pattern = negated ? trimmedLine.Substring(1) : trimmedLine;
                pattern = pattern.Trim('/');
                bool isDirectory = pattern.EndsWith('/');
                if (isDirectory)
                    pattern = pattern.TrimEnd('/');
                
                rules.Add(new GitignoreRule
                {
                    Pattern = pattern ?? string.Empty, // Ensure non-null
                    IsNegated = negated,
                    IsDirectory = isDirectory,
                    BasePath = basePath ?? string.Empty // Ensure non-null
                });
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error reading or parsing .gitignore file {gitignorePath}: {ex.Message}");
            // Return an empty list in case of error to prevent null issues downstream
            return new List<GitignoreRule>(); 
        }
        return rules;
    }

    /// <summary>
    /// Checks if a given path is ignored by the loaded gitignore rules
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <param name="isDirectory">Whether the path is a directory</param>
    /// <param name="rules">The list of gitignore rules to apply. If null or empty, path is not ignored.</param>
    /// <returns>True if the path is ignored, false otherwise</returns>
    public static bool IsPathIgnored(string path, bool isDirectory, List<GitignoreRule>? rules)
    {
        if (rules == null || !rules.Any()) // Handle null or empty rules list
        {
            return false;
        }

        string normalizedPath = Path.GetFullPath(path);
        bool ignored = false;

        foreach (var rule in rules)
        {
            string ruleBasePath = Path.GetFullPath(rule.BasePath);
            string relativePath;

            // Check if the path is within the rule's base path
            if (normalizedPath.StartsWith(ruleBasePath, StringComparison.OrdinalIgnoreCase))
            {
                relativePath = normalizedPath.Substring(ruleBasePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            else
            {
                // If the rule is not anchored (doesn't contain /) it can match anywhere
                // Or if the path is not under the rule's base, it might still match if the rule is global (no base path context)
                // This part might need refinement based on exact .gitignore behavior for rules from parent directories
                relativePath = normalizedPath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).LastOrDefault() ?? string.Empty;
                 // For a simple implementation, if not in base path, consider it not matching this specific rule from a parent .gitignore
                 // unless the rule is a simple filename pattern without any slashes.
                if (rule.Pattern.Contains("/") || rule.Pattern.Contains("\\")) {
                    // If rule pattern has directory separators, it must match from its base path
                    // and we already established it's not within ruleBasePath, so skip.
                    // However, if the rule is like `*.log` (no slashes), it can match anywhere.
                    // This logic is simplified; true gitignore has more complex precedence.
                    if (!IsSimpleFilePattern(rule.Pattern)) continue;
                }
            }

            if (MatchesPattern(relativePath, rule.Pattern, isDirectory, rule.IsDirectory))
            {
                ignored = !rule.IsNegated;
                // Later rules can override earlier ones. Git processes .gitignore from top to bottom,
                // and then from parent .gitignore files. The last matching rule wins.
                // Our current combined list might not perfectly preserve this order if rules from different files overlap significantly.
                // For simplicity, we take the last match.
            }
        }
        return ignored;
    }

    private static bool IsSimpleFilePattern(string pattern)
    {
        return !pattern.Contains('/') && !pattern.Contains('\\');
    }

    /// <summary>
    /// Matches a path against a gitignore pattern
    /// </summary>
    private static bool MatchesPattern(string path, string pattern, bool pathIsDirectory, bool ruleIsDirectory)
    {
        // Convert gitignore pattern to regex
        // This is a simplified conversion and may not cover all gitignore pattern complexities
        string regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*\\*", ".*") // Handle ** for multiple directories
            .Replace("\\*", "[^" + Regex.Escape(Path.DirectorySeparatorChar.ToString()) + Regex.Escape(Path.AltDirectorySeparatorChar.ToString()) + "]*") // Handle * for wildcards
            .Replace("\\?", ".") + "$"; // Handle ? for single character

        // If the rule specifically targets a directory, the path must also be a directory
        if (ruleIsDirectory && !pathIsDirectory)
        {
            // A rule like `bin/` should not match a file named `bin`
            // However, git often treats `bin` as `bin/` if `bin` is a directory.
            // For simplicity here: if rule says dir, path must be dir.
            // A more accurate check might involve seeing if `path + "/"` matches `pattern + "/"`
        }
        
        // A pattern like "foo" can match a file or directory named "foo".
        // A pattern like "foo/" only matches a directory named "foo".
        if (ruleIsDirectory) // Pattern like `logs/`
        {
            // Path must be a directory and match the pattern, or be a path within that directory pattern.
            // e.g. pattern `logs/` should match path `logs` (if it's a dir) or `logs/debug.txt`
            if (pathIsDirectory && Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase)) return true;
            return Regex.IsMatch(path, regexPattern.TrimEnd('$') + "/.*$", RegexOptions.IgnoreCase); // Match `logs/anything`
        }
        else // Pattern like `*.log` or `config.ini`
        {
            return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase);
        }
    }
}

/// <summary>
/// Represents a single rule from a .gitignore file
/// </summary>
public class GitignoreRule
{
    public required string Pattern { get; set; } // Made required
    public bool IsNegated { get; set; }
    public bool IsDirectory { get; set; }
    public required string BasePath { get; set; } // Made required
}
