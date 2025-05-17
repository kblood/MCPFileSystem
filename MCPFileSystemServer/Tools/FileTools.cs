using ModelContextProtocol.Server;
using MCPFileSystemServer.Services;
using MCPFileSystemServer.Models;
using System.ComponentModel;
using System.Text.Json;

namespace MCPFileSystemServer.Tools;

/// <summary>
/// Provides MCP tools for file-related operations.
/// </summary>
[McpServerToolType]
public static class FileTools
{
    /// <summary>
    /// Default JSON serializer options.
    /// </summary>
    private static readonly JsonSerializerOptions DefaultJsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Lists all files in the specified directory.
    /// </summary>
    /// <param name="directoryPath">Absolute path to the directory containing the files.</param>
    /// <returns>A JSON string containing an array of file paths.</returns>
    [McpServerTool("list_files")]
    [Description("Lists all files in the specified directory.")]
    public static string ListFiles(
        [Description("Absolute path to the directory containing the files")]
        string directoryPath,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true) 
    {
        try 
        {
            var files = FileService.ListFiles(directoryPath, respectGitignore);
            if (files.Length > 0 && files[0].StartsWith("Error:")) 
            {
                return JsonSerializer.Serialize(new { error = files[0] }, DefaultJsonOptions);
            }
            return JsonSerializer.Serialize(files, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Lists all files and directories within the specified directory with detailed information.
    /// </summary>
    /// <param name="directoryPath">Path to list contents of.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>JSON string with directories and files in an efficient format.</returns>
    [McpServerTool("list_contents")]
    [Description("Lists all files and directories within the specified directory with detailed information.")]
    public static string ListContents(
        [Description("Path to list contents of")]
        string directoryPath,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true)
    {
        try 
        {
            var contents = FileService.ListDirectoryContents(directoryPath, respectGitignore);
            if (contents.Length > 0 && contents[0].StartsWith("Error:")) 
            {
                return JsonSerializer.Serialize(new { error = contents[0] }, DefaultJsonOptions);
            }
            return JsonSerializer.Serialize(contents, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Reads and returns the contents of a specified file.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to read.</param>
    /// <returns>The contents of the file, or an error message if the file cannot be read.</returns>
    [McpServerTool("read_file")]
    [Description("Reads and returns the contents of a specified file.")]
    public static string ReadFile(
        [Description("Absolute path to the file to read")]
        string filePath)
    {
        var content = FileService.OpenFile(filePath);
        if (content.StartsWith("Error:"))
        {
            return JsonSerializer.Serialize(new { error = content }, DefaultJsonOptions);
        }
        return JsonSerializer.Serialize(new { content }, DefaultJsonOptions);
    }

    /// <summary>
    /// Creates a new file or completely overwrites an existing file with new content.
    /// </summary>
    /// <param name="path">Path to the file to create or overwrite.</param>
    /// <param name="content">Content to write to the file.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("write_file")]
    [Description("Create a new file or completely overwrite an existing file with new content.")]
    public static string WriteFile(
        [Description("Path to the file to create or overwrite")]
        string path,
        [Description("Content to write to the file")]
        string content) =>
        FileService.WriteFile(path, content);

    /// <summary>
    /// Make line-based edits to a text file, including inserting at end or at specific lines.
    /// </summary>
    /// <param name="path">Path to the file to edit.</param>
    /// <param name="oldText">Text to search for and replace. Can be null when using insertMode.</param>
    /// <param name="newText">Text to replace with or insert.</param>
    /// <param name="dryRun">Preview changes using git-style diff format.</param>
    /// <param name="insertMode">How to insert: null for replace, "end" to append to file, or a line number to insert at.</param>
    /// <returns>Success message, diff result, or error message.</returns>
    [McpServerTool("edit_file")]
    [Description("Make line-based edits to a text file, including inserting at end or at specific lines.")]
    public static string EditFile(
        [Description("Path to the file to edit")]
        string path,
        [Description("Text to search for and replace. Can be null when using insertMode")]
        string? oldText,
        [Description("Text to replace with or insert")]
        string newText,
        [Description("Preview changes using git-style diff format")]
        bool dryRun = false,
        [Description("How to insert: null for replace, \"end\" to append to file, or a line number to insert at")]
        object? insertMode = null) =>
        FileService.EditFile(path, oldText, newText, dryRun);

    /// <summary>
    /// Create a new directory or ensure a directory exists.
    /// </summary>
    /// <param name="path">Path for the directory to create.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("create_directory")]
    [Description("Create a new directory or ensure a directory exists.")]
    public static string CreateDirectory(
        [Description("Path for the directory to create")]
        string path) =>
        FileService.CreateDirectory(path);

    /// <summary>
    /// Get a detailed listing of all files and directories in a specified path.
    /// </summary>
    /// <param name="path">Path to list contents of.</param>
    /// <returns>JSON string with directory contents.</returns>
    [McpServerTool("list_directory")]
    [Description("Get a detailed listing of all files and directories in a specified path.")]
    public static string ListDirectory(
        [Description("Path to list contents of")]
        string path,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true) =>
        JsonSerializer.Serialize(FileService.ListDirectories(path, respectGitignore).Concat(FileService.ListFiles(path, respectGitignore)).ToArray(), DefaultJsonOptions);

    /// <summary>
    /// Get a recursive tree view of files and directories as a JSON structure.
    /// </summary>
    /// <param name="path">Path to the root directory.</param>
    /// <returns>JSON string with the directory tree.</returns>
    [McpServerTool("directory_tree")]
    [Description("Get a recursive tree view of files and directories as a JSON structure.")]
    public static string DirectoryTree(
        [Description("Path to the root directory")]
        string path,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true) =>
        JsonSerializer.Serialize(FileService.GetDirectoryTree(path, respectGitignore), DefaultJsonOptions);

    /// <summary>
    /// Move or rename files and directories.
    /// </summary>
    /// <param name="source">Source path.</param>
    /// <param name="destination">Destination path.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("move_file")]
    [Description("Move or rename files and directories.")]
    public static string MoveFile(
        [Description("Source path")]
        string source,
        [Description("Destination path")]
        string destination) =>
        FileService.MoveFile(source, destination);

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    /// <param name="source">Source file path.</param>
    /// <param name="destination">Destination file path.</param>
    /// <param name="overwrite">Whether to overwrite an existing file.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("copy_file")]
    [Description("Copies a file to a new location.")]
    public static string CopyFile(
        [Description("Source file path")]
        string source,
        [Description("Destination file path")]
        string destination,
        [Description("Whether to overwrite an existing file (default: false)")]
        bool overwrite = false) =>
        FileService.CopyFile(source, destination, overwrite);

    /// <summary>
    /// Copies a directory and its contents to a new location.
    /// </summary>
    /// <param name="source">Source directory path.</param>
    /// <param name="destination">Destination directory path.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="recursive">Whether to copy subdirectories.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("copy_directory")]
    [Description("Copies a directory and its contents to a new location.")]
    public static string CopyDirectory(
        [Description("Source directory path")]
        string source,
        [Description("Destination directory path")]
        string destination,
        [Description("Whether to overwrite existing files (default: false)")]
        bool overwrite = false,
        [Description("Whether to copy subdirectories (default: true)")]
        bool recursive = true,
        [Description("Whether to respect .gitignore rules (default: true)")]
        bool respectGitignore = true) =>
        FileService.CopyDirectory(source, destination, overwrite, recursive, respectGitignore);

    /// <summary>
    /// Copies files matching a pattern from source directory to destination directory.
    /// </summary>
    /// <param name="sourceDir">Source directory path.</param>
    /// <param name="destinationDir">Destination directory path.</param>
    /// <param name="pattern">File pattern to match (e.g., "*.exe", "data.*").</param>
    /// <param name="recursive">Whether to search subdirectories recursively.</param>
    /// <param name="overwrite">Whether to overwrite existing files.</param>
    /// <param name="respectGitignore">Whether to respect .gitignore rules.</param>
    /// <returns>Success message with statistics or error message.</returns>
    [McpServerTool("copy_files_with_pattern")]
    [Description("Copies files matching a pattern from source directory to destination directory.")]
    public static string CopyFilesWithPattern(
        [Description("Source directory path")]
        string sourceDir,
        [Description("Destination directory path")]
        string destinationDir,
        [Description("File pattern to match (e.g., '*.exe', 'data.*')")]
        string pattern = "*.*",
        [Description("Whether to search subdirectories recursively (default: false)")]
        bool recursive = false,
        [Description("Whether to overwrite existing files (default: false)")]
        bool overwrite = false,
        [Description("Whether to respect .gitignore rules (default: true)")]
        bool respectGitignore = true) =>
        FileService.CopyFilesWithPattern(sourceDir, destinationDir, pattern, recursive, overwrite, respectGitignore);

    /// <summary>
    /// Recursively search for files and directories matching a pattern.
    /// </summary>
    /// <param name="path">Starting path for the search.</param>
    /// <param name="pattern">Search pattern.</param>
    /// <param name="excludePatterns">Optional patterns to exclude.</param>
    /// <returns>JSON string with matching file paths.</returns>
    [McpServerTool("search_files")]
    [Description("Recursively search for files and directories matching a pattern.")]
    public static string SearchFiles(
        [Description("Starting path for the search")]
        string path,
        [Description("Search pattern")]
        string pattern,
        [Description("Optional patterns to exclude")]
        string[] excludePatterns = null,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true) =>
        JsonSerializer.Serialize(SearchService.SearchFiles(path, pattern, excludePatterns, respectGitignore), DefaultJsonOptions);

    /// <summary>
    /// Retrieve detailed metadata about a file or directory.
    /// </summary>
    /// <param name="path">Path to the file or directory.</param>
    /// <returns>JSON string with file/directory metadata.</returns>
    [McpServerTool("get_file_info")]
    [Description("Retrieve detailed metadata about a file or directory.")]
    public static string GetFileInfo(
        [Description("Path to the file or directory")]
        string path) =>
        JsonSerializer.Serialize(FileService.GetFileInfo(path), DefaultJsonOptions);

    /// <summary>
    /// Sets the base directory for all file operations.
    /// </summary>
    /// <param name="directory">Absolute path to set as the new base directory.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("set_base_directory")]
    [Description("Sets the base directory for all file operations.")]
    public static string SetBaseDirectory(
        [Description("Absolute path to set as the new base directory. Must be a valid, existing directory")]
        string directory)
    {
        try
        {
            FileValidationService.SetBaseDirectory(directory);
            return JsonSerializer.Serialize(new[] { $"Base directory set to: {directory}" }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new[] { $"Error: {ex.Message}" }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Returns the current base directory used for all file operations.
    /// </summary>
    /// <returns>JSON string with the current base directory.</returns>
    [McpServerTool("get_base_directory")]
    [Description("Returns the current base directory used for all file operations.")]
    public static string GetBaseDirectory()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            
            return JsonSerializer.Serialize(new
            {
                BaseDirectory = baseDir,
                Exists = Directory.Exists(baseDir)
            }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Returns the list of directories that this server is allowed to access.
    /// </summary>
    /// <returns>JSON string with allowed directories.</returns>
    [McpServerTool("list_allowed_directories")]
    [Description("Returns the list of directories that this server is allowed to access.")]
    public static string ListAllowedDirectories() =>
        JsonSerializer.Serialize(FileService.ListAccessibleDirectories(), DefaultJsonOptions);

    /// <summary>
    /// Scans the current solution and returns all .csproj files found.
    /// </summary>
    /// <returns>JSON string with project file paths.</returns>
    [McpServerTool("list_projects")]
    [Description("Scans the current solution and returns all .csproj files found.")]
    public static string ListProjects() => 
        JsonSerializer.Serialize(CodeAnalysisService.ListProjects(), DefaultJsonOptions);

    /// <summary>
    /// Searches a specific directory for .csproj files.
    /// </summary>
    /// <param name="directory">Directory to search in.</param>
    /// <returns>JSON string with project file paths.</returns>
    [McpServerTool("list_projects_in_dir")]
    [Description("Searches a specific directory for .csproj files.")]
    public static string ListProjectsInDirectory(
        [Description("Absolute path to the directory to search for .csproj files")]
        string directory) => 
        JsonSerializer.Serialize(CodeAnalysisService.ListProjectsInDirectory(directory), DefaultJsonOptions);

    /// <summary>
    /// Returns all .sln files found in the base directory.
    /// </summary>
    /// <returns>JSON string with solution file paths.</returns>
    [McpServerTool("list_solutions")]
    [Description("Returns all .sln files found in the base directory.")]
    public static string ListSolutions() => 
        JsonSerializer.Serialize(CodeAnalysisService.ListSolutions(), DefaultJsonOptions);

    /// <summary>
    /// Lists all source files in a project directory.
    /// </summary>
    /// <param name="projectDir">Project directory to scan.</param>
    /// <returns>JSON string with source file paths.</returns>
    [McpServerTool("list_source_files")]
    [Description("Lists all source files in a project directory.")]
    public static string ListSourceFiles(
        [Description("Absolute path to the project directory to scan for source files")]
        string projectDir) => 
        JsonSerializer.Serialize(CodeAnalysisService.ListSourceFiles(projectDir), DefaultJsonOptions);

    /// <summary>
    /// Performs a text-based search across all code files for the specified text.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="directory">The directory to search in (defaults to base directory).</param>
    /// <param name="filePattern">Optional file pattern to filter by (e.g., "*.cs").</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>JSON string with search results.</returns>
    [McpServerTool("search_code")]
    [Description("Performs a text-based search across all code files for the specified text.")]
    public static string SearchCode(
        [Description("The exact text string to search for in the codebase")]
        string searchText,
        [Description("Directory to search in (defaults to base directory)")]
        string directory = ".",
        [Description("File pattern to filter by (e.g., '*.cs', defaults to '*.*')")]
        string filePattern = "*.*",
        [Description("Whether to search subdirectories (default: true)")]
        bool recursive = true,
        [Description("Whether to respect .gitignore rules (default: true)")]
        bool respectGitignore = true)
    {
        try
        {
            var results = SearchService.SearchTextInFiles(directory, searchText, filePattern, recursive, respectGitignore);
            
            // Check if there's an error
            if (results.ContainsKey("error"))
            {
                return JsonSerializer.Serialize(new { error = results["error"][0].Text }, DefaultJsonOptions);
            }
            
            // No files found with matches
            if (results.Count == 0)
            {
                return JsonSerializer.Serialize(new { message = $"No matches found for '{searchText}'" }, DefaultJsonOptions);
            }

            return JsonSerializer.Serialize(results, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Searches and replaces text in multiple files.
    /// </summary>
    /// <param name="searchText">The text to search for.</param>
    /// <param name="replaceText">The text to replace with.</param>
    /// <param name="directory">The directory containing the files.</param>
    /// <param name="filePattern">Optional file pattern to filter by.</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <param name="dryRun">Whether to perform a dry run without making changes.</param>
    /// <returns>JSON string with results for each file.</returns>
    [McpServerTool("search_and_replace")]
    [Description("Searches and replaces text in multiple files.")]
    public static string SearchAndReplace(
        [Description("The exact text string to search for")]
        string searchText,
        [Description("The text to replace with")]
        string replaceText,
        [Description("Directory to search in (defaults to base directory)")]
        string directory = ".",
        [Description("File pattern to filter by (e.g., '*.cs', defaults to '*.*')")]
        string filePattern = "*.*",
        [Description("Whether to search subdirectories (default: true)")]
        bool recursive = true,
        [Description("Perform a dry run without making changes (default: false)")]
        bool dryRun = false,
        [Description("Whether to respect .gitignore rules (default: true)")]
        bool respectGitignore = true)
    {
        try
        {
            var results = SearchService.SearchAndReplaceInFiles(directory, searchText, replaceText, filePattern, recursive, dryRun, respectGitignore);
            
            // Check if there's an error
            if (results.ContainsKey("error"))
            {
                return JsonSerializer.Serialize(new { error = results["error"] }, DefaultJsonOptions);
            }
            
            // No files found with matches
            if (results.Count == 0)
            {
                return JsonSerializer.Serialize(new { message = $"No matches found for '{searchText}'" }, DefaultJsonOptions);
            }

            return JsonSerializer.Serialize(results, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Extracts an outline of classes, methods, and properties from C# code files
    /// without including implementation details.
    /// </summary>
    /// <param name="filePath">Path to the C# file to analyze.</param>
    /// <returns>JSON string with the code outline.</returns>
    [McpServerTool("code_outline")]
    [Description("Extracts an outline of classes, methods, and properties from C# code files without implementation details.")]
    public static string GetCodeOutline(
        [Description("Path to the C# file to analyze")]
        string filePath)
    {
        try
        {
            var outline = CodeAnalysisService.GetCodeOutline(filePath);
            return JsonSerializer.Serialize(outline, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Generates code outlines for all C# files in a directory.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to analyze.</param>
    /// <param name="filePattern">File pattern to filter (default: *.cs).</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>JSON string with code outlines for all files.</returns>
    [McpServerTool("code_outline_directory")]
    [Description("Generates code outlines for all C# files in a directory.")]
    public static string GetCodeOutlineDirectory(
        [Description("Path to the directory to analyze")]
        string directoryPath, 
        [Description("File pattern to filter (default: *.cs)")]
        string filePattern = "*.cs",
        [Description("Whether to search subdirectories (default: true)")]
        bool recursive = true)
    {
        try
        {
            var outlines = CodeAnalysisService.GetCodeOutlinesForDirectory(directoryPath, filePattern, recursive);
            return JsonSerializer.Serialize(outlines, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Formats a file size in bytes to a human-readable string.
    /// </summary>
    /// <param name="sizeInBytes">Size in bytes.</param>
    /// <returns>Formatted file size (e.g., "1.5 MB", "500 KB").</returns>
    public static string GetFileSize(long sizeInBytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;
        const long TB = GB * 1024;

        return sizeInBytes switch
        {
            < KB => $"{sizeInBytes} B",
            < MB => $"{Math.Round((double)sizeInBytes / KB, 1)} KB",
            < GB => $"{Math.Round((double)sizeInBytes / MB, 1)} MB",
            < TB => $"{Math.Round((double)sizeInBytes / GB, 1)} GB",
            _ => $"{Math.Round((double)sizeInBytes / TB, 1)} TB"
        };
    }
}
