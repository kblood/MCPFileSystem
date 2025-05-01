using System.Text;
using MCPFileSystemServer.Models;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Provides search-related operations for the file system.
/// </summary>
public static class SearchService
{
    /// <summary>
    /// Searches for text in all files within a directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="filePattern">Optional file pattern to filter by (e.g., "*.cs").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>A dictionary with search results.</returns>
    public static Dictionary<string, List<SearchMatch>> SearchTextInFiles(string directory, string searchText, string filePattern = "*.*", bool recursive = true, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new Dictionary<string, List<SearchMatch>> 
                {
                    ["error"] = new List<SearchMatch> { new SearchMatch { Line = -1, Text = $"Directory not found: {directory}" } }
                };
            }

            if (string.IsNullOrEmpty(searchText))
            {
                return new Dictionary<string, List<SearchMatch>> 
                {
                    ["error"] = new List<SearchMatch> { new SearchMatch { Line = -1, Text = "Search text cannot be empty" } }
                };
            }

            var results = new Dictionary<string, List<SearchMatch>>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(normalizedPath, filePattern, searchOption);
            
            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
            }

            foreach (var file in files)
            {
                try
                {
                    // Skip files that match gitignore patterns
                    if (respectGitignore && GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                    {
                        continue;
                    }
                    
                    // Skip binary files or files that are too large
                    var fileInfo = new System.IO.FileInfo(file);
                    if (fileInfo.Length > 10 * 1024 * 1024) // Skip files larger than 10MB
                    {
                        continue;
                    }

                    // Read text file and search line by line
                    var fileMatches = new List<SearchMatch>();
                    string[] lines = File.ReadAllLines(file);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            fileMatches.Add(new SearchMatch
                            {
                                Line = i + 1,
                                Text = lines[i].Trim()
                            });
                        }
                    }

                    if (fileMatches.Count > 0)
                    {
                        results[file] = fileMatches;
                    }
                }
                catch (Exception ex)
                {
                    // Skip files that can't be read
                    continue;
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, List<SearchMatch>> 
            {
                ["error"] = new List<SearchMatch> { new SearchMatch { Line = -1, Text = ex.Message } }
            };
        }
    }

    /// <summary>
    /// Searches and replaces text in multiple files.
    /// </summary>
    /// <param name="directory">The directory containing the files.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="replaceText">The text to replace with.</param>
    /// <param name="filePattern">Optional file pattern to filter by (e.g., "*.cs").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>A dictionary with results for each file.</returns>
    public static Dictionary<string, string> SearchAndReplaceInFiles(string directory, string searchText, string replaceText, string filePattern = "*.*", bool recursive = true, bool dryRun = false, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new Dictionary<string, string> { ["error"] = $"Directory not found: {directory}" };
            }

            if (string.IsNullOrEmpty(searchText))
            {
                return new Dictionary<string, string> { ["error"] = "Search text cannot be empty" };
            }

            var results = new Dictionary<string, string>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(normalizedPath, filePattern, searchOption);
            
            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
            }

            foreach (var file in files)
            {
                try
                {
                    // Skip files that match gitignore patterns
                    if (respectGitignore && GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                    {
                        continue;
                    }
                    
                    // Skip binary files or files that are too large
                    var fileInfo = new System.IO.FileInfo(file);
                    if (fileInfo.Length > 10 * 1024 * 1024) // Skip files larger than 10MB
                    {
                        results[file] = "Skipped (file too large)";
                        continue;
                    }

                    string content = File.ReadAllText(file);
                    if (!content.Contains(searchText))
                    {
                        continue; // Skip files that don't contain the search text
                    }

                    string newContent = content.Replace(searchText, replaceText);
                    int replacements = CountStringOccurrences(content, searchText);

                    if (dryRun)
                    {
                        var diff = GenerateDiff(content, newContent);
                        results[file] = $"Found {replacements} occurrences. Diff:\n{diff}";
                    }
                    else
                    {
                        File.WriteAllText(file, newContent);
                        results[file] = $"Replaced {replacements} occurrences";
                    }
                }
                catch (Exception ex)
                {
                    results[file] = $"Error: {ex.Message}";
                }
            }

            return results;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, string> { ["error"] = ex.Message };
        }
    }

    /// <summary>
    /// Searches for files matching a pattern.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="pattern">The search pattern.</param>
    /// <param name="excludePatterns">Optional patterns to exclude.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>An array of file paths matching the search criteria.</returns>
    public static string[] SearchFiles(string directory, string pattern, string[] excludePatterns = null, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directory}" };
            }
            
            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
            }

            var files = Directory.GetFiles(normalizedPath, $"*{pattern}*", SearchOption.AllDirectories);
            
            // Apply filters
            var filteredFiles = files.Where(file =>
            {
                // Check gitignore rules
                if (respectGitignore && gitignoreRules != null && 
                    GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                {
                    return false;
                }
                
                // Check exclude patterns
                if (excludePatterns != null && excludePatterns.Length > 0)
                {
                    var fileName = Path.GetFileName(file);
                    if (excludePatterns.Any(p => fileName.Contains(p)))
                    {
                        return false;
                    }
                }
                
                return true;
            }).ToArray();

            return filteredFiles;
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Generates a line-based diff between two text contents.
    /// </summary>
    /// <param name="oldContent">The original content.</param>
    /// <param name="newContent">The new content.</param>
    /// <returns>A string representation of the differences.</returns>
    public static string GenerateDiff(string oldContent, string newContent)
    {
        // Simple line-based diff
        var oldLines = oldContent.Split('\n');
        var newLines = newContent.Split('\n');
        var diff = new StringBuilder();

        // Find the first differing line
        var minLength = Math.Min(oldLines.Length, newLines.Length);
        for (int i = 0; i < minLength; i++)
        {
            if (oldLines[i] != newLines[i])
            {
                diff.AppendLine($"@@ Line {i + 1} @@");
                diff.AppendLine($"- {oldLines[i]}");
                diff.AppendLine($"+ {newLines[i]}");
                
                // Show a few lines of context
                var contextStart = Math.Max(0, i - 2);
                var contextEnd = Math.Min(minLength, i + 3);
                
                if (contextStart < i)
                {
                    diff.AppendLine("Context:");
                    for (int j = contextStart; j < i; j++)
                    {
                        diff.AppendLine($"  {oldLines[j]}");
                    }
                }
                
                if (i + 1 < contextEnd)
                {
                    diff.AppendLine("Context (after):");
                    for (int j = i + 1; j < contextEnd; j++)
                    {
                        diff.AppendLine($"  {oldLines[j]}");
                    }
                }
                
                break;
            }
        }

        // If lengths differ, note that too
        if (oldLines.Length != newLines.Length)
        {
            diff.AppendLine($"@@ Line count changed from {oldLines.Length} to {newLines.Length} @@");
        }

        return diff.ToString();
    }

    /// <summary>
    /// Counts occurrences of a pattern in text.
    /// </summary>
    /// <param name="text">The text to search in.</param>
    /// <param name="pattern">The pattern to search for.</param>
    /// <returns>The number of occurrences.</returns>
    public static int CountStringOccurrences(string text, string pattern)
    {
        // Count string occurrences
        int count = 0;
        int i = 0;
        while ((i = text.IndexOf(pattern, i, StringComparison.OrdinalIgnoreCase)) != -1)
        {
            i += pattern.Length;
            count++;
        }
        return count;
    }
}
