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
    /// <returns>A list of gitignore rules</returns>
    public static List<GitignoreRule> LoadGitignoreRules(string directory)
    {
        string normalizedDir = Path.GetFullPath(directory);
        
        // Check cache first
        if (_gitignoreCache.TryGetValue(normalizedDir, out var cachedRules))
        {
            return cachedRules;
        }
        
        var rules = new List<GitignoreRule>();
        
        // Find all gitignore files in this directory and parent directories
        string currentDir = normalizedDir;
        
        while (!string.IsNullOrEmpty(currentDir))
        {
            string gitignorePath = Path.Combine(currentDir, ".gitignore");
            
            if (File.Exists(gitignorePath))
            {
                try
                {
                    var gitignoreRules = ParseGitignore(gitignorePath, currentDir);
                    rules.AddRange(gitignoreRules);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error parsing .gitignore at {gitignorePath}: {ex.Message}");
                }
            }
            
            // Move up to parent directory (stop when we reach the root)
            string parentDir = Path.GetDirectoryName(currentDir);
            if (parentDir == currentDir)
                break;
                
            currentDir = parentDir;
        }
        
        // Cache the rules
        _gitignoreCache[normalizedDir] = rules;
        
        return rules;
    }
    
    /// <summary>
    /// Parses a .gitignore file and returns a list of rules
    /// </summary>
    /// <param name="gitignorePath">Path to the .gitignore file</param>
    /// <param name="basePath">Base path where the .gitignore file is located</param>
    /// <returns>List of GitignoreRule objects</returns>
    private static List<GitignoreRule> ParseGitignore(string gitignorePath, string basePath)
    {
        var rules = new List<GitignoreRule>();
        var lines = File.ReadAllLines(gitignorePath);
        
        foreach (var line in lines)
        {
            // Skip empty lines and comments
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#"))
                continue;
            
            bool negated = trimmedLine.StartsWith("!");
            string pattern = negated ? trimmedLine.Substring(1) : trimmedLine;
            
            // Remove leading and trailing slashes
            pattern = pattern.Trim('/');
            
            // Determine if it's a directory pattern (ends with /)
            bool isDirectory = pattern.EndsWith('/');
            if (isDirectory)
                pattern = pattern.TrimEnd('/');
            
            // Add the rule
            rules.Add(new GitignoreRule
            {
                Pattern = pattern,
                IsNegated = negated,
                IsDirectory = isDirectory,
                BasePath = basePath
            });
        }
        
        return rules;
    }
    
    /// <summary>
    /// Checks if a path should be ignored based on the gitignore rules
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <param name="isDirectory">Whether the path is a directory</param>
    /// <param name="rules">List of gitignore rules to apply</param>
    /// <returns>True if the path should be ignored, false otherwise</returns>
    public static bool IsPathIgnored(string path, bool isDirectory, List<GitignoreRule> rules)
    {
        if (rules == null || rules.Count == 0)
            return false;
            
        string normalizedPath = Path.GetFullPath(path);
        bool shouldIgnore = false;
        
        // Check each rule in order
        foreach (var rule in rules)
        {
            if (rule.Matches(normalizedPath, isDirectory))
            {
                // Negated rules override previous ignore decisions
                shouldIgnore = !rule.IsNegated;
            }
        }
        
        return shouldIgnore;
    }
}

/// <summary>
/// Represents a single rule in a .gitignore file
/// </summary>
public class GitignoreRule
{
    /// <summary>
    /// The pattern string from the gitignore file
    /// </summary>
    public string Pattern { get; set; }
    
    /// <summary>
    /// Whether this is a negated pattern (starts with !)
    /// </summary>
    public bool IsNegated { get; set; }
    
    /// <summary>
    /// Whether this pattern is for directories only (ends with /)
    /// </summary>
    public bool IsDirectory { get; set; }
    
    /// <summary>
    /// The base path where the .gitignore file containing this rule is located
    /// </summary>
    public string BasePath { get; set; }
    
    /// <summary>
    /// Determines if this rule matches the given path
    /// </summary>
    /// <param name="path">The path to check</param>
    /// <param name="isDirectory">Whether the path is a directory</param>
    /// <returns>True if the rule matches the path</returns>
    public bool Matches(string path, bool isDirectory)
    {
        // Directory-only patterns should only match directories
        if (IsDirectory && !isDirectory)
            return false;
            
        // Get the relative path from the base path of the .gitignore file
        string relativePath = GetRelativePath(path);
        string fileName = Path.GetFileName(path);
        
        // Convert the gitignore pattern to a regex pattern
        string regexPattern = GitignorePatternToRegex(Pattern);
        
        // Check if the pattern matches the full path, the relative path, or just the filename
        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase) || 
               Regex.IsMatch(relativePath, regexPattern, RegexOptions.IgnoreCase) ||
               Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase);
    }
    
    private string GetRelativePath(string path)
    {
        // Make sure both paths end with directory separator
        string normalizedBasePath = BasePath.EndsWith(Path.DirectorySeparatorChar) ? 
            BasePath : BasePath + Path.DirectorySeparatorChar;
            
        // If the path is not under the base path, return the original path
        if (!path.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            return path;
            
        return path.Substring(normalizedBasePath.Length);
    }
    
    private string GitignorePatternToRegex(string pattern)
    {
        // Special case for '**' which matches any directory depth
        pattern = pattern.Replace("**", "__DOUBLEWILDCARD__");
        
        // Escape special regex characters except * and ?
        pattern = Regex.Escape(pattern);
        
        // Replace gitignore wildcards with regex equivalents
        pattern = pattern.Replace("\\*", "[^/]*");      // * matches anything except /
        pattern = pattern.Replace("\\?", "[^/]");       // ? matches a single character except /
        pattern = pattern.Replace("__DOUBLEWILDCARD__", ".*"); // ** matches anything including /
        
        // Anchor the pattern to match the whole string
        if (!pattern.StartsWith("^"))
            pattern = "^" + pattern;
            
        if (!pattern.EndsWith("$"))
            pattern += "$";
            
        return pattern;
    }
}
