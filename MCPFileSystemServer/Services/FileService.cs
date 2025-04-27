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
    /// <returns>An array of file paths or an error message.</returns>
    public static string[] ListFiles(string directoryPath)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directoryPath}" };
            }

            var files = Directory.GetFiles(normalizedPath);
            return files;
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
    /// <returns>An array of directory paths or an error message.</returns>
    public static string[] ListDirectories(string directoryPath)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directoryPath}" };
            }

            var directories = Directory.GetDirectories(normalizedPath);
            return directories;
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all .csproj files in the base directory.
    /// </summary>
    /// <returns>An array of .csproj file paths.</returns>
    public static string[] ListProjects()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            return Directory.GetFiles(baseDir, "*.csproj", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all .csproj files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <returns>An array of .csproj file paths.</returns>
    public static string[] ListProjectsInDirectory(string directory)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directory}" };
            }

            return Directory.GetFiles(normalizedPath, "*.csproj", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all .sln files in the base directory.
    /// </summary>
    /// <returns>An array of .sln file paths.</returns>
    public static string[] ListSolutions()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            return Directory.GetFiles(baseDir, "*.sln", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all source files in a project directory.
    /// </summary>
    /// <param name="projectDir">The project directory to search in.</param>
    /// <returns>An array of source file paths.</returns>
    public static string[] ListSourceFiles(string projectDir)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(projectDir);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {projectDir}" };
            }

            // Common source file extensions
            var extensions = new[] { "*.cs", "*.vb", "*.fs", "*.ts", "*.js", "*.html", "*.css", "*.sql", "*.json", "*.xml" };
            var files = new List<string>();

            foreach (var extension in extensions)
            {
                files.AddRange(Directory.GetFiles(normalizedPath, extension, SearchOption.AllDirectories));
            }

            // Filter out common binary folders
            var result = files.Where(f => !f.Contains("\\bin\\") && 
                                         !f.Contains("\\obj\\") && 
                                         !f.Contains("\\node_modules\\")).ToArray();

            return result;
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
    /// Edits a file by replacing specified text.
    /// </summary>
    /// <param name="filePath">The path of the file to edit.</param>
    /// <param name="oldText">The text to replace.</param>
    /// <param name="newText">The new text.</param>
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

            var content = File.ReadAllText(normalizedPath);
            
            if (!content.Contains(oldText))
            {
                return $"Error: Old text not found in file: {filePath}";
            }

            var newContent = content.Replace(oldText, newText);

            if (dryRun)
            {
                // Generate a simple diff
                var diff = SearchService.GenerateDiff(content, newContent);
                return $"Dry run diff:\n{diff}";
            }

            File.WriteAllText(normalizedPath, newContent);
            return $"Successfully edited file {filePath}";
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
    /// <returns>A nested structure representing the directory tree.</returns>
    public static object GetDirectoryTree(string path)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(path);

            if (!Directory.Exists(normalizedPath))
            {
                return new { error = $"Directory not found: {path}" };
            }

            return BuildDirectoryTree(new System.IO.DirectoryInfo(normalizedPath));
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

    private static object BuildDirectoryTree(System.IO.DirectoryInfo directoryInfo)
    {
        var result = new Dictionary<string, object>
        {
            ["name"] = directoryInfo.Name,
            ["type"] = "directory",
            ["children"] = new List<object>()
        };

        // Add subdirectories
        foreach (var subDir in directoryInfo.GetDirectories())
        {
            ((List<object>)result["children"]).Add(BuildDirectoryTree(subDir));
        }

        // Add files
        foreach (var file in directoryInfo.GetFiles())
        {
            ((List<object>)result["children"]).Add(new Dictionary<string, object>
            {
                ["name"] = file.Name,
                ["type"] = "file"
            });
        }

        return result;
    }

    #endregion
}
