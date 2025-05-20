using System.Text;
using System.Text.RegularExpressions; // Added for Regex
using MCPFileSystem.Contracts; // Changed from MCPFileSystemServer.Models
using System.IO; // Added for Path, Directory, File, SearchOption etc.
using System.Collections.Generic; // Added for List, Dictionary
using System.Linq; // Added for Linq operations
using System.Threading.Tasks; // Added for Task

namespace MCPFileSystemServer.Services;

/// <summary>
/// Provides search-related operations for the file system.
/// </summary>
public static class SearchService
{
    // New SearchAsync method to be called by FileService
    public static async Task<IEnumerable<MCPFileSystem.Contracts.SearchResult>> SearchAsync(
        string query,
        string? filePattern = null,
        bool caseSensitive = false,
        bool useRegex = false,
        string? searchPath = null) // searchPath is relative to FileValidationService.BaseDirectory
    {
        string baseSearchPath = FileValidationService.NormalizePath(searchPath ?? string.Empty);
        var results = new List<MCPFileSystem.Contracts.SearchResult>();

        // This is a simplified implementation. A more robust one would handle async file I/O properly.
        await Task.Run(() =>
        {
            var files = Directory.GetFiles(baseSearchPath, filePattern ?? "*.*", SearchOption.AllDirectories);
            List<GitignoreRule> gitignoreRules = GitignoreService.LoadGitignoreRules(baseSearchPath);

            foreach (var filePath in files)
            {
                if (GitignoreService.IsPathIgnored(filePath, false, gitignoreRules))
                {
                    continue;
                }

                var fileInfo = new System.IO.FileInfo(filePath);
                if (fileInfo.Length > 10 * 1024 * 1024) // Skip large files
                {
                    continue;
                }

                var matches = new List<MCPFileSystem.Contracts.SearchMatch>();
                try
                {
                    string[] lines = File.ReadAllLines(filePath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        bool isMatch = false;
                        if (useRegex)
                        {
                            isMatch = Regex.IsMatch(lines[i], query, caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        }
                        else
                        {
                            isMatch = lines[i].Contains(query, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
                        }

                        if (isMatch)
                        {
                            matches.Add(new MCPFileSystem.Contracts.SearchMatch
                            {
                                Line = i + 1,
                                Text = lines[i].Trim()
                            });
                        }
                    }

                    if (matches.Any())
                    {
                        string relativeFilePath = filePath; // filePath is absolute
                        if (filePath.StartsWith(FileValidationService.BaseDirectory, StringComparison.OrdinalIgnoreCase))
                        {
                            relativeFilePath = filePath.Substring(FileValidationService.BaseDirectory.Length);
                            relativeFilePath = relativeFilePath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                        }
                        // else: path is not under BaseDirectory, preserve it or log an error if this state is unexpected.

                        results.Add(new MCPFileSystem.Contracts.SearchResult
                        {
                            Path = relativeFilePath,
                            Matches = matches,
                            Type = "file" 
                        });
                    }
                }
                catch (Exception) 
                {
                    // Skip files that can't be read or processed
                }
            }
        });
        return results;
    }


    /// <summary>
    /// Searches for text in all files within a directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="filePattern">Optional file pattern to filter by (e.g., "*.cs").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>A dictionary with search results.</returns>
    public static Dictionary<string, List<MCPFileSystem.Contracts.SearchMatch>> SearchTextInFiles(string directory, string searchText, string filePattern = "*.*", bool recursive = true, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new Dictionary<string, List<MCPFileSystem.Contracts.SearchMatch>>
                {
                    ["error"] = new List<MCPFileSystem.Contracts.SearchMatch> { new MCPFileSystem.Contracts.SearchMatch { Line = -1, Text = $"Directory not found: {directory}" } }
                };
            }

            if (string.IsNullOrEmpty(searchText))
            {
                return new Dictionary<string, List<MCPFileSystem.Contracts.SearchMatch>>
                {
                    ["error"] = new List<MCPFileSystem.Contracts.SearchMatch> { new MCPFileSystem.Contracts.SearchMatch { Line = -1, Text = "Search text cannot be empty" } }
                };
            }

            var results = new Dictionary<string, List<MCPFileSystem.Contracts.SearchMatch>>();
            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(normalizedPath, filePattern, searchOption);
            
            List<GitignoreRule> gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);

            foreach (var file in files)
            {
                try
                {
                    if (respectGitignore && GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                    {
                        continue;
                    }
                    
                    var fileInfo = new System.IO.FileInfo(file);
                    if (fileInfo.Length > 10 * 1024 * 1024) 
                    {
                        continue;
                    }

                    var fileMatches = new List<MCPFileSystem.Contracts.SearchMatch>();
                    string[] lines = File.ReadAllLines(file);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains(searchText, StringComparison.OrdinalIgnoreCase))
                        {
                            fileMatches.Add(new MCPFileSystem.Contracts.SearchMatch
                            {
                                Line = i + 1,
                                Text = lines[i].Trim()
                            });
                        }
                    }

                    if (fileMatches.Count > 0)
                    {
                        // Path returned in dictionary key should be relative to FileValidationService.BaseDirectory
                        string relativeKeyPath = file.StartsWith(FileValidationService.BaseDirectory) ? 
                                                 file.Substring(FileValidationService.BaseDirectory.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) :
                                                 file;
                        results[relativeKeyPath] = fileMatches;
                    }
                }
                catch (Exception) 
                {
                    continue;
                }
            }
            return results;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, List<MCPFileSystem.Contracts.SearchMatch>>
            {
                ["error"] = new List<MCPFileSystem.Contracts.SearchMatch> { new MCPFileSystem.Contracts.SearchMatch { Line = -1, Text = ex.Message } }
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
            
            // Ensure gitignoreRules is always initialized to a non-null list.
            List<GitignoreRule> gitignoreRules = respectGitignore 
                                                ? GitignoreService.LoadGitignoreRules(normalizedPath) 
                                                : new List<GitignoreRule>();
            // If LoadGitignoreRules could somehow still return null (it shouldn't based on its code), ensure it's an empty list.
            if (gitignoreRules == null) 
            {
                gitignoreRules = new List<GitignoreRule>();
            }

            foreach (var file in files)
            {
                try
                {
                    // No need for gitignoreRules null check here if it's guaranteed to be non-null by initialization.
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
    public static string[] SearchFiles(string directory, string pattern, string[]? excludePatterns = null, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return Array.Empty<string>(); // Changed: Return empty array
            }
            
            // Initialize with an empty list if no .gitignore file is found or if respectGitignore is false.
            List<GitignoreRule> gitignoreRules = respectGitignore ? GitignoreService.LoadGitignoreRules(normalizedPath) : new List<GitignoreRule>();


            var filesToSearch = Directory.GetFiles(normalizedPath, pattern, SearchOption.AllDirectories); // Changed: Use pattern directly
            
            var filteredFiles = filesToSearch.Where(file =>
            {
                // Ensure gitignoreRules is not null before calling IsPathIgnored
                if (respectGitignore && gitignoreRules != null && GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                {
                    return false;
                }
                
                if (excludePatterns != null && excludePatterns.Length > 0)
                {
                    var fileName = Path.GetFileName(file);
                    if (excludePatterns.Any(p => fileName.Contains(p))) // Existing logic, consider if needs more robust matching
                    {
                        return false;
                    }
                }
                
                return true;
            })
            .Select(absPath => { // Changed: Make paths relative to BaseDirectory
                string relPath = absPath;
                if (absPath.StartsWith(FileValidationService.BaseDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    relPath = absPath.Substring(FileValidationService.BaseDirectory.Length);
                    relPath = relPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                }
                return relPath;
            })
            .ToArray();

            return filteredFiles;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in SearchFiles (\"{directory}\", \"{pattern}\"): {ex.Message}"); // Changed: Log error
            return Array.Empty<string>(); // Changed: Return empty array
        }
    }

    /// <summary>
    /// Asynchronously searches for files matching a pattern.
    /// </summary>
    /// <param name="directory">The directory to search in (relative to BaseDirectory or absolute if validated).</param>
    /// <param name="pattern">The search pattern (e.g., \"*.txt\").</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <param name="excludePatterns">Optional patterns to exclude files.</param>
    /// <returns>An array of relative file paths matching the search criteria.</returns>
    public static async Task<string[]> SearchFilesAsync(string directory, string pattern, bool respectGitignore, string[]? excludePatterns = null)
    {
        return await Task.Run(() =>
        {
            try
            {
                // NormalizePath expects a path relative to BaseDirectory or an allowed absolute path.
                // If 'directory' is intended to be always relative to BaseDirectory, it should be prefixed.
                // For now, assuming 'directory' is already a validated, accessible path.
                string validatedPath = FileValidationService.NormalizePath(directory);

                if (!Directory.Exists(validatedPath))
                {
                    // Consider logging this or throwing a specific exception
                    return Array.Empty<string>();
                }

                List<GitignoreRule> gitignoreRules = respectGitignore ? GitignoreService.LoadGitignoreRules(validatedPath) : new List<GitignoreRule>();
                if (gitignoreRules == null) gitignoreRules = new List<GitignoreRule>(); // Ensure not null

                var filesToSearch = Directory.EnumerateFiles(validatedPath, pattern, SearchOption.AllDirectories);

                var filteredFiles = filesToSearch.Where(filePath =>
                {
                    if (respectGitignore && GitignoreService.IsPathIgnored(filePath, false, gitignoreRules))
                    {
                        return false;
                    }

                    if (excludePatterns != null && excludePatterns.Length > 0)
                    {
                        var fileName = Path.GetFileName(filePath);
                        // Simple exclusion, might need more robust globbing for exclude patterns
                        if (excludePatterns.Any(p => fileName.Contains(p, StringComparison.OrdinalIgnoreCase) || 
                                                     (p.StartsWith("*.") && fileName.EndsWith(p.Substring(1), StringComparison.OrdinalIgnoreCase))))
                        {
                            return false;
                        }
                    }
                    return true;
                })
                .Select(absPath =>
                {
                    // Ensure paths are relative to the original 'directory' parameter if it was relative,
                    // or to the BaseDirectory if 'directory' was absolute but within it.
                    // For simplicity and consistency with other parts, let's make it relative to BaseDirectory.
                    string relPath = absPath;
                    if (absPath.StartsWith(FileValidationService.BaseDirectory, StringComparison.OrdinalIgnoreCase))
                    {
                        relPath = absPath.Substring(FileValidationService.BaseDirectory.Length);
                        relPath = relPath.TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    }
                    // If absPath is not under BaseDirectory but was validated (e.g. an explicitly allowed path),
                    // then the path should remain absolute or be made relative to that allowed path.
                    // Current FileValidationService primarily works with one BaseDirectory.
                    // This logic assumes paths are within or relative to BaseDirectory.
                    return relPath;
                })
                .ToArray();

                return filteredFiles;
            }
            catch (Exception ex)
            {
                // Log the exception (e.g., using a proper logging framework)
                Console.Error.WriteLine($"Error in SearchFilesAsync (directory: \"{directory}\", pattern: \"{pattern}\"): {ex.Message}");
                return Array.Empty<string>(); // Return empty on error
            }
        });
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
