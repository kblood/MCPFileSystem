using MCPFileSystemServer.Tools;
using System.Text;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Provides file system operations for the MCP server.
/// </summary>
public static class FileService
{
    /// <summary>
    /// Lists all files in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory to list files from.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>An array of file paths or an error message.</returns>
    public static string[] ListFiles(string directoryPath, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directoryPath}" };
            }

            // Get directory info first
            var dirInfo = new DirectoryInfo(normalizedPath);
            List<string> result = new List<string>
            {
                $"Directory: {dirInfo.FullName}",
                $"Created: {dirInfo.CreationTime}",
                $"Last Modified: {dirInfo.LastWriteTime}"
            };

            // Get files
            var files = Directory.GetFiles(normalizedPath);
            
            // Apply gitignore filtering if requested
            if (respectGitignore)
            {
                var gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
                files = files.Where(file => !GitignoreService.IsPathIgnored(file, false, gitignoreRules)).ToArray();
            }

            // Add file count
            result.Add($"File count: {files.Length}");
            
            // Add files to the result
            result.AddRange(files);
            
            return result.ToArray();
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all directories in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory to list directories from.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>An array of directory paths or an error message.</returns>
    public static string[] ListDirectories(string directoryPath, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directoryPath}" };
            }

            // Get directory info first
            var dirInfo = new DirectoryInfo(normalizedPath);
            List<string> result = new List<string>
            {
                $"Parent Directory: {dirInfo.FullName}",
                $"Created: {dirInfo.CreationTime}",
                $"Last Modified: {dirInfo.LastWriteTime}"
            };

            // Get subdirectories
            var directories = Directory.GetDirectories(normalizedPath);
            
            // Apply gitignore filtering if requested
            if (respectGitignore)
            {
                var gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
                directories = directories.Where(dir => !GitignoreService.IsPathIgnored(dir, true, gitignoreRules)).ToArray();
            }
            
            // Add directory count
            result.Add($"Subdirectory count: {directories.Length}");
            
            // Add directories to the result
            result.AddRange(directories);
            
            return result.ToArray();
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all files and directories in the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory to list contents from.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>An array of content information or an error message.</returns>
    public static string[] ListDirectoryContents(string directoryPath, bool respectGitignore = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directoryPath}" };
            }

            // Get directory info first
            var dirInfo = new DirectoryInfo(normalizedPath);
            List<string> result = new List<string>
            {
                $"Directory: {dirInfo.FullName}",
                $"Created: {dirInfo.CreationTime}",
                $"Last Modified: {dirInfo.LastWriteTime}"
            };

            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedPath);
            }

            // Get subdirectories
            var directories = Directory.GetDirectories(normalizedPath);
            
            // Apply gitignore filtering if requested
            if (respectGitignore)
            {
                directories = directories.Where(dir => 
                    !GitignoreService.IsPathIgnored(dir, true, gitignoreRules)).ToArray();
            }
            
            // Get files
            var files = Directory.GetFiles(normalizedPath);
            
            // Apply gitignore filtering if requested
            if (respectGitignore)
            {
                files = files.Where(file => 
                    !GitignoreService.IsPathIgnored(file, false, gitignoreRules)).ToArray();
            }

            // Add counts
            result.Add($"Subdirectory count: {directories.Length}");
            result.Add($"File count: {files.Length}");
            result.Add("---");
            
            // Add directories with folder indicator
            result.Add("FOLDERS:");
            if (directories.Length > 0)
            {
                foreach (var dir in directories)
                {
                    result.Add($"📁 {Path.GetFileName(dir)}");
                }
            }
            else
            {
                result.Add("(No folders)");
            }
            
            result.Add("---");
            
            // Add files
            result.Add("FILES:");
            if (files.Length > 0)
            {
                foreach (var file in files)
                {
                    var fileInfo = new FileInfo(file);
                    result.Add($"📄 {Path.GetFileName(file)} ({FileTools.GetFileSize(fileInfo.Length)})");
                }
            }
            else
            {
                result.Add("(No files)");
            }
            
            return result.ToArray();
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Reads a file and returns its contents.
    /// </summary>
    /// <param name="filePath">The path of the file to read.</param>
    /// <returns>The file contents as a string.</returns>
    public static string OpenFile(string filePath)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(filePath);

            if (!File.Exists(normalizedPath))
            {
                return $"Error: File not found: {filePath}";
            }

            return File.ReadAllText(normalizedPath);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Writes content to a file.
    /// </summary>
    /// <param name="filePath">The path of the file to write.</param>
    /// <param name="content">The content to write.</param>
    /// <returns>Success message or error message.</returns>
    public static string WriteFile(string filePath, string content)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(filePath);

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(normalizedPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(normalizedPath, content);
            return $"Successfully wrote to {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Creates a directory.
    /// </summary>
    /// <param name="path">The path of the directory to create.</param>
    /// <returns>Success message or error message.</returns>
    public static string CreateDirectory(string path)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(path);

            if (Directory.Exists(normalizedPath))
            {
                return $"Directory already exists: {path}";
            }

            Directory.CreateDirectory(normalizedPath);
            return $"Successfully created directory {path}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Moves a file or directory to a new location.
    /// </summary>
    /// <param name="source">The source path.</param>
    /// <param name="destination">The destination path.</param>
    /// <returns>Success message or error message.</returns>
    public static string MoveFile(string source, string destination)
    {
        try
        {
            var normalizedSource = FileValidationService.NormalizePath(source);
            var normalizedDestination = FileValidationService.NormalizePath(destination);

            // Check if source exists
            if (!File.Exists(normalizedSource) && !Directory.Exists(normalizedSource))
            {
                return $"Error: Source path not found: {source}";
            }

            // Check if destination already exists
            if (File.Exists(normalizedDestination) || Directory.Exists(normalizedDestination))
            {
                return $"Error: Destination path already exists: {destination}";
            }

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(normalizedDestination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            // Move file or directory
            if (File.Exists(normalizedSource))
            {
                File.Move(normalizedSource, normalizedDestination);
                return $"Successfully moved file from {source} to {destination}";
            }
            else
            {
                Directory.Move(normalizedSource, normalizedDestination);
                return $"Successfully moved directory from {source} to {destination}";
            }
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    /// <param name="source">The source file path.</param>
    /// <param name="destination">The destination file path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <returns>Success message or error message.</returns>
    public static string CopyFile(string source, string destination, bool overwrite = false)
    {
        try
        {
            var normalizedSource = FileValidationService.NormalizePath(source);
            var normalizedDestination = FileValidationService.NormalizePath(destination);

            // Check if source exists and is a file
            if (!File.Exists(normalizedSource))
            {
                return $"Error: Source file not found: {source}";
            }

            // Check if destination already exists and overwrite is false
            if (File.Exists(normalizedDestination) && !overwrite)
            {
                return $"Error: Destination file already exists: {destination}. Use overwrite=true to replace it.";
            }

            // Ensure destination directory exists
            var destDir = Path.GetDirectoryName(normalizedDestination);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(normalizedSource, normalizedDestination, overwrite);
            return $"Successfully copied file from {source} to {destination}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies a directory and its contents to a new location.
    /// </summary>
    /// <param name="source">The source directory path.</param>
    /// <param name="destination">The destination directory path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="recursive">Whether to copy subdirectories.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>Success message or error message.</returns>
    public static string CopyDirectory(string source, string destination, bool overwrite = false, bool recursive = true, bool respectGitignore = true)
    {
        try
        {
            var normalizedSource = FileValidationService.NormalizePath(source);
            var normalizedDestination = FileValidationService.NormalizePath(destination);

            // Check if source exists and is a directory
            if (!Directory.Exists(normalizedSource))
            {
                return $"Error: Source directory not found: {source}";
            }

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(normalizedDestination))
            {
                Directory.CreateDirectory(normalizedDestination);
            }

            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedSource);
            }

            // Get files in source directory
            var files = Directory.GetFiles(normalizedSource);
            int copiedFiles = 0;
            int copiedDirs = 0;

            // Copy each file
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(normalizedDestination, fileName);

                // Skip if file is ignored by gitignore
                if (respectGitignore && GitignoreService.IsPathIgnored(file, false, gitignoreRules))
                {
                    continue;
                }

                File.Copy(file, destFile, overwrite);
                copiedFiles++;
            }

            // Copy subdirectories if recursive is true
            if (recursive)
            {
                var dirs = Directory.GetDirectories(normalizedSource);
                foreach (var dir in dirs)
                {
                    var dirName = Path.GetFileName(dir);
                    var destDir = Path.Combine(normalizedDestination, dirName);

                    // Skip if directory is ignored by gitignore
                    if (respectGitignore && GitignoreService.IsPathIgnored(dir, true, gitignoreRules))
                    {
                        continue;
                    }

                    var result = CopyDirectory(dir, destDir, overwrite, recursive, respectGitignore);
                    
                    // If successful, increment count (extract the count from result)
                    if (!result.StartsWith("Error:"))
                    {
                        copiedDirs++;
                    }
                }
            }

            return $"Successfully copied directory from {source} to {destination}. Copied {copiedFiles} files and {copiedDirs} directories.";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Copies files matching a pattern from source to destination directory.
    /// </summary>
    /// <param name="sourceDir">The source directory to copy files from.</param>
    /// <param name="destinationDir">The destination directory to copy files to.</param>
    /// <param name="pattern">File pattern to match (e.g., "*.exe", "data.*").</param>
    /// <param name="recursive">Whether to search subdirectories recursively.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>Result message with statistics about the copied files.</returns>
    public static string CopyFilesWithPattern(
        string sourceDir, 
        string destinationDir, 
        string pattern = "*.*", 
        bool recursive = false,
        bool overwrite = false,
        bool respectGitignore = true)
    {
        try
        {
            var normalizedSourceDir = FileValidationService.NormalizePath(sourceDir);
            var normalizedDestDir = FileValidationService.NormalizePath(destinationDir);

            // Check if source directory exists
            if (!Directory.Exists(normalizedSourceDir))
            {
                return $"Error: Source directory not found: {sourceDir}";
            }

            // Create destination directory if it doesn't exist
            if (!Directory.Exists(normalizedDestDir))
            {
                Directory.CreateDirectory(normalizedDestDir);
            }

            // Load gitignore rules if needed
            List<GitignoreRule> gitignoreRules = null;
            if (respectGitignore)
            {
                gitignoreRules = GitignoreService.LoadGitignoreRules(normalizedSourceDir);
            }

            // Find all matching files
            var files = Directory.GetFiles(normalizedSourceDir, pattern,
                recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            
            // Apply gitignore filtering if requested
            if (respectGitignore && gitignoreRules != null)
            {
                files = files.Where(file => !GitignoreService.IsPathIgnored(file, false, gitignoreRules)).ToArray();
            }

            if (files.Length == 0)
            {
                return $"No files matching pattern '{pattern}' found in {sourceDir}";
            }

            // Copy each matching file
            int successCount = 0;
            int failCount = 0;
            var results = new StringBuilder();
            
            foreach (var file in files)
            {
                try
                {
                    // Calculate relative path to maintain directory structure if copying recursively
                    string relativePath = Path.GetRelativePath(normalizedSourceDir, file);
                    string destinationFile = Path.Combine(normalizedDestDir, relativePath);
                    
                    // Ensure destination directory exists for the file
                    string destinationFileDir = Path.GetDirectoryName(destinationFile);
                    if (!Directory.Exists(destinationFileDir))
                    {
                        Directory.CreateDirectory(destinationFileDir);
                    }

                    // Skip if file already exists and overwrite is false
                    if (File.Exists(destinationFile) && !overwrite)
                    {
                        results.AppendLine($"Skipped (already exists): {relativePath}");
                        continue;
                    }

                    // Copy the file
                    File.Copy(file, destinationFile, overwrite);
                    successCount++;
                    results.AppendLine($"Copied: {relativePath}");
                }
                catch (Exception ex)
                {
                    failCount++;
                    results.AppendLine($"Failed to copy {Path.GetFileName(file)}: {ex.Message}");
                }
            }

            // Build summary
            var summary = new StringBuilder();
            summary.AppendLine($"Copied {successCount} files from {sourceDir} to {destinationDir}");
            if (failCount > 0)
            {
                summary.AppendLine($"Failed to copy {failCount} files");
            }
            summary.AppendLine(results.ToString());
            
            return summary.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Edits a file by replacing specified text.
    /// </summary>
    /// <param name="filePath">The path of the file to edit.</param>
    /// <param name="oldText">The text to replace.</param>
    /// <param name="newText">The new text to replace with.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>Success message, diff result, or error message.</returns>
    public static string EditFile(string filePath, string oldText, string newText, bool dryRun = false)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(filePath);

            if (!File.Exists(normalizedPath))
            {
                return $"Error: File not found: {filePath}";
            }

            // Read the original content
            var originalContent = File.ReadAllText(normalizedPath);
            string newContent;

            // Only support replacement mode
            if (string.IsNullOrEmpty(oldText))
            {
                return $"Error: Old text cannot be null or empty in replacement mode.";
            }

            if (!originalContent.Contains(oldText))
            {
                return $"Error: Old text not found in file: {filePath}";
            }

            newContent = originalContent.Replace(oldText, newText);

            if (dryRun)
            {
                // Generate a simple diff
                var diff = SearchService.GenerateDiff(originalContent, newContent);
                return $"Dry run diff:\n{diff}";
            }

            File.WriteAllText(normalizedPath, newContent);
            return $"Successfully replaced text in file {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Inserts text into a file at a specific line or at the end.
    /// </summary>
    /// <param name="filePath">The path of the file to insert into.</param>
    /// <param name="newText">The new text to insert.</param>
    /// <param name="insertMode">"end" to append, or a line number to insert at.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>Success message, diff result, or error message.</returns>
    public static string InsertIntoFile(string filePath, string newText, object? insertMode = null, bool dryRun = false)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(filePath);

            if (!File.Exists(normalizedPath))
            {
                return $"Error: File not found: {filePath}";
            }

            // Read the original content
            var originalContent = File.ReadAllText(normalizedPath);
            string newContent;

            if (insertMode is string strInsertMode && strInsertMode.Equals("end", StringComparison.OrdinalIgnoreCase))
            {
                // Append to end mode
                newContent = originalContent;
                if (!string.IsNullOrEmpty(originalContent) &&
                    !originalContent.EndsWith("\n") &&
                    !originalContent.EndsWith("\r"))
                {
                    newContent += Environment.NewLine;
                }
                newContent += newText;
            }
            else if (insertMode is int lineNum || (insertMode is string strLineNum && int.TryParse(strLineNum, out lineNum)))
            {
                // Line number mode
                var lines = File.ReadAllLines(normalizedPath).ToList();
                if (lineNum < 0 || lineNum > lines.Count)
                {
                    return $"Error: Line number {lineNum} is out of range. File has {lines.Count} lines.";
                }
                lines.Insert(lineNum, newText);
                newContent = string.Join(Environment.NewLine, lines);
            }
            else
            {
                return $"Error: Invalid insertMode: {insertMode}. Use \"end\" to append, or a line number.";
            }

            if (dryRun)
            {
                var diff = SearchService.GenerateDiff(originalContent, newContent);
                return $"Dry run diff:\n{diff}";
            }

            File.WriteAllText(normalizedPath, newContent);

            string modeDescription = insertMode is string && ((string)insertMode).Equals("end") ? "appended to" : $"inserted at line {insertMode} in";
            return $"Successfully {modeDescription} file {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    /// Gets a recursive directory tree as a JSON-serializable object.
    /// </summary>
    /// <param name="path">The root directory path.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <param name="showHidden">Whether to include hidden files and directories.</param>
    /// <param name="includeGitFolders">Whether to include .git folders.</param>
    /// <returns>A nested structure representing the directory tree.</returns>
    public static object GetDirectoryTree(string path, bool respectGitignore = true, bool showHidden = false, bool includeGitFolders = false)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(path);

            if (!Directory.Exists(normalizedPath))
            {
                return new { error = $"Directory not found: {path}" };
            }

            return BuildDirectoryTree(new System.IO.DirectoryInfo(normalizedPath), respectGitignore, showHidden, includeGitFolders);
        }
        catch (Exception ex)
        {
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Gets metadata about a file or directory.
    /// </summary>
    /// <param name="path">The path to get info for.</param>
    /// <returns>A dictionary with file or directory information.</returns>
    public static Dictionary<string, object> GetFileInfo(string path)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(path);
            var result = new Dictionary<string, object>();

            if (File.Exists(normalizedPath))
            {
                var fileInfo = new System.IO.FileInfo(normalizedPath);
                result["type"] = "file";
                result["name"] = fileInfo.Name;
                result["path"] = normalizedPath;
                result["size"] = fileInfo.Length;
                result["created"] = fileInfo.CreationTime;
                result["modified"] = fileInfo.LastWriteTime;
                result["attributes"] = fileInfo.Attributes.ToString();
                result["extension"] = fileInfo.Extension;
            }
            else if (Directory.Exists(normalizedPath))
            {
                var dirInfo = new System.IO.DirectoryInfo(normalizedPath);
                result["type"] = "directory";
                result["name"] = dirInfo.Name;
                result["path"] = normalizedPath;
                result["created"] = dirInfo.CreationTime;
                result["modified"] = dirInfo.LastWriteTime;
                result["attributes"] = dirInfo.Attributes.ToString();
                result["fileCount"] = Directory.GetFiles(normalizedPath).Length;
                result["directoryCount"] = Directory.GetDirectories(normalizedPath).Length;
            }
            else
            {
                result["error"] = $"Path not found: {path}";
            }

            return result;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    /// <summary>
    /// Lists all accessible directories.
    /// </summary>
    /// <returns>An array of accessible directory paths.</returns>
    public static string[] ListAccessibleDirectories()
    {
        return FileValidationService.AccessibleDirectories.ToArray();
    }

    #region Helper Methods

    private static object BuildDirectoryTree(System.IO.DirectoryInfo directoryInfo, bool respectGitignore = true, bool showHidden = false, bool includeGitFolders = false)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = directoryInfo.Name,
            ["path"] = directoryInfo.FullName,
            ["type"] = "directory",
            ["created"] = directoryInfo.CreationTime,
            ["modified"] = directoryInfo.LastWriteTime
        };

        // Load gitignore rules if needed
        List<GitignoreRule> gitignoreRules = null;
        if (respectGitignore)
        {
            gitignoreRules = GitignoreService.LoadGitignoreRules(directoryInfo.FullName);
        }

        // Process subdirectories
        var subdirectories = directoryInfo.GetDirectories()
            .Where(subDir => 
                (includeGitFolders || !subDir.Name.Equals(".git", StringComparison.OrdinalIgnoreCase)) &&
                (showHidden || (!subDir.Attributes.HasFlag(FileAttributes.Hidden) && !subDir.Name.StartsWith("."))) &&
                (!respectGitignore || !GitignoreService.IsPathIgnored(subDir.FullName, true, gitignoreRules!)))
            .ToList();
            
        // Process files
        var files = directoryInfo.GetFiles()
            .Where(file => 
                (showHidden || (!file.Attributes.HasFlag(FileAttributes.Hidden) && !file.Name.StartsWith("."))) &&
                (!respectGitignore || !GitignoreService.IsPathIgnored(file.FullName, false, gitignoreRules!)))
            .ToList();

        // Add directories and files count
        result["directories"] = subdirectories.Count;
        result["files"] = files.Count;
        
        // Prepare list with directories first, then files
        var children = new List<object>();
        
        // Add subdirectories
        foreach (var subDir in subdirectories)
        {
            children.Add(BuildDirectoryTree(subDir, respectGitignore, showHidden, includeGitFolders));
        }
        
        // Add files more efficiently - just name and size
        foreach (var file in files)
        {
            children.Add(new Dictionary<string, object>
            {
                ["name"] = file.Name,
                ["type"] = "file",
                ["size"] = FileTools.GetFileSize(file.Length)
            });
        }
        
        result["children"] = children;
        return result;
    }

    #endregion
}
