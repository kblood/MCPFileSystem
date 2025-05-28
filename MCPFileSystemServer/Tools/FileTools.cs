using ModelContextProtocol.Server;
using MCPFileSystemServer.Services;
using System.ComponentModel;
using System.Text.Json;
using MCPFileSystem.Contracts;
using System.Linq;
using System.Threading.Tasks; // Added for Task
using System.Collections.Generic; // Added for List

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
        WriteIndented = true,
        // Handle potential nulls gracefully if needed, though contracts should be robust
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull 
    };

    private static FileService GetFileService()
    {
        // Ensure FileValidationService.BaseDirectory is initialized before creating FileService instance.
        // This might require an explicit initialization call if not already done,
        // or ensuring SetBaseDirectory tool is called first by the client.
        // For now, assuming BaseDirectory is valid.
        if (string.IsNullOrEmpty(FileValidationService.BaseDirectory))
        {
            // Attempt to set a default if possible, or throw a clear exception.
            // This depends on application design. For now, let's assume it must be set.
            throw new InvalidOperationException("BaseDirectory is not set. Please call set_base_directory first.");
        }
        return new FileService(FileValidationService.BaseDirectory);
    }

    /// <summary>
    /// Lists all files in the specified directory.
    /// </summary>
    /// <param name=\"directoryPath\">Absolute path to the directory containing the files.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules.</param>
    /// <returns>A JSON string containing an array of file paths.</returns>
    [McpServerTool("list_files")]
    [Description("Lists all files (non-recursively) in the specified directory, returning their paths.")]
    public static async Task<string> ListFiles(
        [Description("Absolute path to the directory")]
        string directoryPath,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true)
    {
        try
        {
            var fileService = GetFileService();
            var contents = await fileService.ListDirectoryContentsAsync(directoryPath, respectGitignore);
            var filePaths = contents.Where(entry => !entry.IsDirectory).Select(entry => entry.Path);
            return JsonSerializer.Serialize(filePaths, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Lists all files and directories within the specified directory with detailed information.
    /// </summary>
    /// <param name=\"directoryPath\">Path to list contents of.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules.</param>
    /// <returns>JSON string with directories and files in an efficient format.</returns>
    [McpServerTool("list_contents")]
    [Description("Lists all files and directories within the specified directory with detailed information.")]
    public static async Task<string> ListContents(
        [Description("Path to list contents of")]
        string directoryPath,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true)
    {
        try
        {
            var fileService = GetFileService();
            var contents = await fileService.ListDirectoryContentsAsync(directoryPath, respectGitignore);
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
    /// <param name=\"filePath\">Absolute path to the file to read.</param>
    /// <param name=\"startLine\">Optional start line (1-based).</param>
    /// <param name=\"endLine\">Optional end line (1-based).</param>
    /// <returns>The contents of the file, or an error message if the file cannot be read.</returns>
    [McpServerTool("read_file")]
    [Description("Reads and returns the contents of a specified file.")]
    public static async Task<string> ReadFile(
        [Description("Absolute path to the file to read")]
        string filePath,
        [Description("Optional start line (1-based) to read from.")]
        int? startLine = null,
        [Description("Optional end line (1-based) to read up to (inclusive).")]
        int? endLine = null)
    {
        try
        {
            var fileService = GetFileService();
            ReadFileResponse response = await fileService.ReadFileAsync(filePath, startLine, endLine);
            return JsonSerializer.Serialize(response, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Creates a new file or completely overwrites an existing file with new content.
    /// </summary>
    /// <param name=\"path\">Path to the file to create or overwrite.</param>
    /// <param name=\"content\">Content to write to the file.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("write_file")]
    [Description("Create a new file or completely overwrite an existing file with new content.")]
    public static async Task<string> WriteFile(
        [Description("Path to the file to create or overwrite")]
        string path,
        [Description("Content to write to the file")]
        string content)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.WriteFileAsync(path, content);
            // After writing, get file info to return SHA and other details
            var fileInfo = await fileService.GetFileInfoAsync(path);
            var sha = string.Empty;
            if (fileInfo.Exists) // Attempt to get SHA if file was successfully written
            {
                 // FileService.GetFileInfoAsync doesn't return SHA. ReadFileResponse does.
                 // To get SHA, we might need to call ReadFileAsync for the first line or a helper.
                 // For simplicity, let's assume WriteFileAsync could return some confirmation.
                 // Or, we can call ReadFileAsync with minimal range if SHA is strictly needed here.
                 // Let's call ReadFileAsync to get the SHA.
                var readResponse = await fileService.ReadFileAsync(path, 1, 1); // Read first line to get SHA
                sha = readResponse.FileSHA;
            }
            return JsonSerializer.Serialize(new { message = "File written successfully.", path = path, sha = sha }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Creates a new file or completely overwrites an existing file with new content, with encoding options.
    /// </summary>
    /// <param name=\"path\">Path to the file to create or overwrite.</param>
    /// <param name=\"content\">Content to write to the file.</param>
    /// <param name=\"encodingOptionsJson\">JSON string with encoding options. Format: {\"Encoding\":\"Utf8NoBom\",\"PreserveOriginalEncoding\":false}</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("write_file_with_encoding")]
    [Description("Create a new file or completely overwrite an existing file with new content, with encoding options.")]
    public static async Task<string> WriteFileWithEncoding(
        [Description("Path to the file to create or overwrite")]
        string path,
        [Description("Content to write to the file")]
        string content,
        [Description("JSON string with encoding options. Format: {\"Encoding\":\"Utf8NoBom\",\"PreserveOriginalEncoding\":false}. Encoding options: Utf8NoBom, Utf8WithBom, Ascii, Utf16Le, Utf16Be, Utf32Le, SystemDefault, AutoDetect")]
        string? encodingOptionsJson = null)
    {
        try
        {
            var fileService = GetFileService();
            
            // Parse encoding options
            FileWriteOptions? options = null;
            if (!string.IsNullOrEmpty(encodingOptionsJson))
            {
                try
                {
                    options = JsonSerializer.Deserialize<FileWriteOptions>(encodingOptionsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException jsonEx)
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid JSON format for encoding options: {jsonEx.Message}" }, DefaultJsonOptions);
                }
            }

            // Use encoding-aware WriteFileAsync if options provided, otherwise use default
            if (options != null)
            {
                await fileService.WriteFileAsync(path, content, options);
            }
            else
            {
                await fileService.WriteFileAsync(path, content);
            }

            // After writing, get file info to return SHA and other details
            var fileInfo = await fileService.GetFileInfoAsync(path);
            var sha = string.Empty;
            string? detectedEncoding = null;
            
            if (fileInfo.Exists)
            {
                var readResponse = await fileService.ReadFileAsync(path, 1, 1); // Read first line to get SHA and encoding
                sha = readResponse.FileSHA;
                detectedEncoding = readResponse.Encoding;
            }
            
            return JsonSerializer.Serialize(new 
            { 
                message = "File written successfully with encoding support.", 
                path = path, 
                sha = sha,
                encoding = detectedEncoding,
                options = options
            }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }    /// <summary>
    /// Make replace-based edits to a text file.
    /// </summary>
    /// <param name=\"path\">Path to the file to edit.</param>
    /// <param name=\"editsJson\">A JSON string representing a list of replace edits to apply.</param>
    /// <param name=\"dryRun\">Preview changes using git-style diff format.</param>
    /// <returns>Success message, diff result, or error message.</returns>
    [McpServerTool("edit_file")]
    [Description("Make replace-based edits to a text file. Only Replace operations are supported. Edits should be a JSON string: '[{\"LineNumber\":1,\"Type\":\"Replace\",\"Text\":\"new content\",\"OldText\":\"old content\"}]'")]
    public static async Task<string> EditFile(
        [Description("Path to the file to edit")]
        string path,
        [Description("A JSON string representing a list of replace edits to apply. Each edit must specify LineNumber (1-based), Type (must be 'Replace'), Text (replacement text), and optionally OldText (specific text to replace within the line).")]
        string editsJson, // Changed to string to accept JSON
        [Description("Preview changes without applying them")]
        bool dryRun = false)
    {        try
        {
            // Use the improved JSON helper for better validation and error messages
            var (edits, validationErrors) = FileEditJsonHelper.DeserializeEdits(editsJson);
            
            if (validationErrors.Any())
            {
                var errorMessage = $"Validation errors: {string.Join("; ", validationErrors)}";
                
                // Add helpful example for common issues
                if (validationErrors.Any(e => e.Contains("newline") || e.Contains("Unterminated")))
                {
                    errorMessage += "\n\nExample of correct multi-line JSON:\n" +
                                   "[{\"LineNumber\":1,\"Type\":\"Replace\",\"Text\":\"line1\\nline2\"}]";
                }
                
                return JsonSerializer.Serialize(new { error = errorMessage }, DefaultJsonOptions);
            }

            if (edits == null || !edits.Any())
            {
                return JsonSerializer.Serialize(new { error = "No valid edits provided." }, DefaultJsonOptions);
            }
            
            var fileService = GetFileService();
            EditResult result = await fileService.EditFileAsync(path, edits, dryRun);
            return JsonSerializer.Serialize(result, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Create a new directory or ensure a directory exists.
    /// </summary>
    /// <param name=\"path\">Path for the directory to create.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("create_directory")]
    [Description("Create a new directory or ensure a directory exists.")]
    public static async Task<string> CreateDirectory(
        [Description("Path for the directory to create")]
        string path)
    {
        try
        {
            var fileService = GetFileService();
            DirectoryInfoContract dirInfo = await fileService.CreateDirectoryAsync(path);
            // Corrected: Use FullName instead of Path, as DirectoryInfoContract has FullName
            return JsonSerializer.Serialize(new { message = "Directory created successfully.", path = dirInfo.FullName, name = dirInfo.Name, creationTime = dirInfo.CreationTime }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Get a detailed listing of all files and directories in a specified path. (This is similar to ListContents)
    /// </summary>
    /// <param name=\"path\">Path to list contents of.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules.</param>
    /// <returns>JSON string with directory contents.</returns>
    [McpServerTool("list_directory")]
    [Description("Get a detailed listing of all files and directories in a specified path.")]
    public static async Task<string> ListDirectory(
        [Description("Path to list contents of")]
        string path,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true) =>
        await ListContents(path, respectGitignore); // Delegate to ListContents

    /// <summary>
    /// Get a recursive tree view of files and directories as a JSON structure.
    /// </summary>
    /// <param name=\"path\">Path to the root directory.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules.</param>
    /// <param name=\"maxDepth\">Maximum depth to traverse.</param>
    /// <param name=\"includeFiles\">Whether to include files in the tree.</param>
    /// <returns>JSON string with the directory tree.</returns>
    [McpServerTool("directory_tree")]
    [Description("Get a recursive tree view of files and directories as a JSON structure.")]
    public static async Task<string> DirectoryTree(
        [Description("Path to the root directory")]
        string path,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true,
        [Description("Maximum depth to traverse. Default is 3.")]
        int maxDepth = 3,
        [Description("Whether to include files in the tree. Default is true.")]
        bool includeFiles = true)
    {
        try
        {
            var fileService = GetFileService();
            DirectoryTreeNode treeNode = await fileService.GetDirectoryTreeAsync(path, respectGitignore, maxDepth, includeFiles);
            return JsonSerializer.Serialize(treeNode, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Move or rename files and directories.
    /// </summary>
    /// <param name=\"sourcePath\">Source path.</param>
    /// <param name=\"destinationPath\">Destination path.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("move_item")]
    [Description("Move or rename files and directories.")]
    public static async Task<string> MoveItem(
        [Description("Source path")]
        string sourcePath,
        [Description("Destination path")]
        string destinationPath)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.MovePathAsync(sourcePath, destinationPath);
            return JsonSerializer.Serialize(new { message = $"Item moved successfully from '{sourcePath}' to '{destinationPath}'." }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Copies a file to a new location.
    /// </summary>
    /// <param name=\"sourcePath\">Source file path.</param>
    /// <param name=\"destinationPath\">Destination file path.</param>
    /// <param name=\"overwrite\">Whether to overwrite an existing file.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("copy_file")]
    [Description("Copies a file to a new location.")]
    public static async Task<string> CopyFile(
        [Description("Source file path")]
        string sourcePath,
        [Description("Destination file path")]
        string destinationPath,
        [Description("Whether to overwrite an existing file (default: false)")]
        bool overwrite = false)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.CopyFileAsync(sourcePath, destinationPath, overwrite);
            return JsonSerializer.Serialize(new { message = $"File copied successfully from '{sourcePath}' to '{destinationPath}'." }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Copies a directory and its contents to a new location.
    /// </summary>
    /// <param name=\"sourcePath\">Source directory path.</param>
    /// <param name=\"destinationPath\">Destination directory path.</param>
    /// <param name=\"overwrite\">Whether to overwrite existing files.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules when copying contents.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("copy_directory")]
    [Description("Copies a directory and its contents to a new location.")]
    public static async Task<string> CopyDirectory(
        [Description("Source directory path")]
        string sourcePath,
        [Description("Destination directory path")]
        string destinationPath,
        [Description("Whether to overwrite existing files (default: false)")]
        bool overwrite = false,
        [Description("Whether to respect .gitignore rules when copying contents (default: true)")]
        bool respectGitignore = true)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.CopyDirectoryAsync(sourcePath, destinationPath, overwrite, respectGitignore);
            return JsonSerializer.Serialize(new { message = $"Directory copied successfully from '{sourcePath}' to '{destinationPath}'." }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Deletes a file or an empty directory. For non-empty directories, use delete_directory_recursive.
    /// </summary>
    /// <param name=\"path\">Path to the file or empty directory to delete.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("delete_item")]
    [Description("Deletes a file or an empty directory. For non-empty directories, use delete_directory_recursive.")]
    public static async Task<string> DeleteItem(
        [Description("Path to the file or empty directory to delete")]
        string path)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.DeletePathAsync(path, false); // Not recursive by default for this tool
            return JsonSerializer.Serialize(new { message = $"Item '{path}' deleted successfully." }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Deletes a directory and all its contents recursively.
    /// </summary>
    /// <param name=\"path\">Path to the directory to delete.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("delete_directory_recursive")]
    [Description("Deletes a directory and all its contents recursively.")]
    public static async Task<string> DeleteDirectoryRecursive(
        [Description("Path to the directory to delete")]
        string path)
    {
        try
        {
            var fileService = GetFileService();
            await fileService.DeletePathAsync(path, true); // Recursive delete
            return JsonSerializer.Serialize(new { message = $"Directory '{path}' deleted recursively successfully." }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Recursively search for files matching a pattern.
    /// </summary>
    /// <param name=\"directory\">Starting path for the search.</param>
    /// <param name=\"pattern\">Search pattern (e.g., \"*.txt\").</param>
    /// <param name=\"excludePatternsJson\">Optional JSON string array of patterns to exclude files.</param>
    /// <param name=\"respectGitignore\">Whether to respect .gitignore rules.</param>
    /// <returns>JSON string with matching file paths.</returns>
    [McpServerTool("search_files_by_pattern")]
    [Description("Recursively search for files matching a pattern.")]
    public static async Task<string> SearchFilesByPattern(
        [Description("Starting directory for the search")]
        string directory,
        [Description("Search pattern (e.g., \'*.txt\')")]
        string pattern,
        [Description("Optional JSON string array of patterns to exclude files (e.g., '[\\\"*.log\\\", \\\"temp_*\\\"]'. Pass null or empty string if no exclusions.)")]
        string? excludePatternsJson = null,
        [Description("Whether to respect .gitignore rules")]
        bool respectGitignore = true)
    {
        try
        {
            string[]? excludePatterns = null;
            if (!string.IsNullOrEmpty(excludePatternsJson))
            {
                try
                {
                    excludePatterns = JsonSerializer.Deserialize<string[]>(excludePatternsJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException jsonEx)
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid JSON format for exclude patterns: {jsonEx.Message}" }, DefaultJsonOptions);
                }
            }

            // SearchService.SearchFilesAsync returns Task<string[]>
            string[] files = await SearchService.SearchFilesAsync(directory, pattern, respectGitignore, excludePatterns);
            return JsonSerializer.Serialize(files, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Retrieve detailed metadata about a file or directory.
    /// </summary>
    /// <param name=\"path\">Path to the file or directory.</param>
    /// <returns>JSON string with file/directory metadata.</returns>
    [McpServerTool("get_item_info")]
    [Description("Retrieve detailed metadata about a file or directory.")]
    public static async Task<string> GetItemInfo(
        [Description("Path to the file or directory")]
        string path)
    {
        try
        {
            var fileService = GetFileService();
            // Corrected: GetPathInfoAsync returns PathInfo. PathInfo is a FileSystemEntry.
            // No explicit cast needed if PathInfo derives from FileSystemEntry and contains all necessary fields for serialization.
            // If FileSystemEntry is preferred for the variable type for some reason, and PathInfo is assignable, it's fine.
            // Let's use PathInfo directly as it's the specific type returned.
            PathInfo info = await fileService.GetPathInfoAsync(path);
            return JsonSerializer.Serialize(info, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Sets the base directory for all file operations. This directory must exist.
    /// </summary>
    /// <param name=\"directory\">Absolute path to set as the new base directory.</param>
    /// <returns>Success or error message.</returns>
    [McpServerTool("set_base_directory")]
    [Description("Sets the base directory for all file operations. This directory must exist.")]
    public static string SetBaseDirectory( // This remains synchronous as FileValidationService.SetBaseDirectory is sync
        [Description("Absolute path to set as the new base directory. Must be a valid, existing directory")]
        string directory)
    {
        try
        {
            // FileValidationService.SetBaseDirectory will throw if the directory doesn't exist or is invalid.
            FileValidationService.SetBaseDirectory(directory);
            // To ensure FileService instances created after this use the new path, 
            // GetFileService() will naturally pick it up.
            return JsonSerializer.Serialize(new { message = $"Base directory set to: {FileValidationService.BaseDirectory}" }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Returns the current base directory used for all file operations.
    /// </summary>
    /// <returns>JSON string with the current base directory.</returns>
    [McpServerTool("get_base_directory")]
    [Description("Returns the current base directory used for all file operations.")]
    public static string GetBaseDirectory() // This remains synchronous
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            return JsonSerializer.Serialize(new
            {
                BaseDirectory = baseDir,
                Exists = !string.IsNullOrEmpty(baseDir) && System.IO.Directory.Exists(baseDir)
            }, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            // This should ideally not throw if BaseDirectory is just a string property.
            return JsonSerializer.Serialize(new { Error = $"Failed to retrieve base directory: {ex.Message}" }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Returns the list of directories that this server is allowed to access.
    /// </summary>
    /// <returns>JSON string with allowed directories.</returns>
    [McpServerTool("list_allowed_directories")]
    [Description("Returns the list of directories that this server is allowed to access (includes the current BaseDirectory).")]
    public static string ListAllowedDirectories() // This remains synchronous
    {
        try
        {
            var allowedDirs = FileValidationService.AllowedBaseDirectories;
            return JsonSerializer.Serialize(allowedDirs, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Searches for text in files within a directory.
    /// </summary>
    /// <param name=\"query\">The text or regex pattern to search for.</param>
    /// <param name=\"searchPath\">Optional relative path within the base directory to search. Defaults to base directory.</param>
    /// <param name=\"filePattern\">Optional file pattern (e.g., \"*.cs\"). Defaults to all files.</param>
    /// <param name=\"caseSensitive\">Whether the search is case-sensitive. Default is false.</param>
    /// <param name=\"useRegex\">Whether the query is a regular expression. Default is false.</param>
    /// <returns>A JSON string containing search results.</returns>
    [McpServerTool("search_text_in_files")]
    [Description("Searches for text in files. Can use regex and specify case sensitivity.")]
    public static async Task<string> SearchTextInFiles(
        [Description("The text or regex pattern to search for.")]
        string query,
        [Description("Optional relative path within the base directory to search. Defaults to base directory (empty string or null).")]
        string? searchPath = null,
        [Description("Optional file pattern (e.g., \\\"*.cs\\\"). Defaults to all files (*.*).")]
        string? filePattern = null,
        [Description("Whether the search is case-sensitive. Default is false.")]
        bool caseSensitive = false,
        [Description("Whether the query is a regular expression. Default is false.")]
        bool useRegex = false)
    {
        try
        {
            // SearchService.SearchAsync returns Task<IEnumerable<SearchResult>>
            var results = await SearchService.SearchAsync(query, filePattern, caseSensitive, useRegex, searchPath);
            return JsonSerializer.Serialize(results, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    // Tools for CodeAnalysisService (static calls, remain synchronous as per CodeAnalysisService current design)

    /// <summary>
    /// Lists all .csproj files in the current base directory and its subdirectories.
    /// </summary>
    [McpServerTool("list_projects")]
    [Description("Lists all .csproj files in the current base directory and its subdirectories.")]
    public static string ListProjects() // Sync
    {
        try
        {
            // CodeAnalysisService.ListProjects() uses FileValidationService.BaseDirectory internally
            var projects = CodeAnalysisService.ListProjects();
            return JsonSerializer.Serialize(projects, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Lists all .csproj files in the specified directory (must be within allowed paths).
    /// </summary>
    [McpServerTool("list_projects_in_directory")]
    [Description("Lists all .csproj files in the specified directory (must be within allowed paths).")]
    public static string ListProjectsInDirectory(
        [Description("The directory to search in (e.g., 'src/MyProject'). Path is relative to base or an allowed absolute path.")] string directory) // Sync
    {
        try
        {
            var projects = CodeAnalysisService.ListProjectsInDirectory(directory);
            return JsonSerializer.Serialize(projects, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Lists all .sln files in the current base directory and its subdirectories.
    /// </summary>
    [McpServerTool("list_solutions")]
    [Description("Lists all .sln files in the current base directory and its subdirectories.")]
    public static string ListSolutions() // Sync
    {
        try
        {
            // CodeAnalysisService.ListSolutions() uses FileValidationService.BaseDirectory internally
            var solutions = CodeAnalysisService.ListSolutions();
            return JsonSerializer.Serialize(solutions, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Lists all source files in a project directory (must be within allowed paths).
    /// </summary>
    [McpServerTool("list_source_files")]
    [Description("Lists all source files in a project directory (must be within allowed paths).")]
    public static string ListSourceFiles(
        [Description("The project directory to search in (e.g., 'src/MyProject'). Path is relative to base or an allowed absolute path.")] string projectDir) // Sync
    {
        try
        {
            var files = CodeAnalysisService.ListSourceFiles(projectDir);
            return JsonSerializer.Serialize(files, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }

    /// <summary>
    /// Extracts an outline of classes, methods, and properties from a C# code file using Roslyn.
    /// </summary>
    [McpServerTool("get_code_outline")]
    [Description("Extracts an outline of classes, methods, and properties from a C# code file using Roslyn.")]
    public static string GetCodeOutline(
        [Description("Path to the C# file to analyze (e.g., 'src/MyClass.cs'). Path is relative to base or an allowed absolute path.")] string filePath) // Sync
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
    /// Generate code outline for all C# files in a directory using Roslyn.
    /// </summary>
    [McpServerTool("get_code_outlines_for_directory")]
    [Description("Generate code outline for all C# files in a directory using Roslyn.")]
    public static string GetCodeOutlinesForDirectory(
        [Description("Path to the directory to analyze (e.g., 'src/Services'). Path is relative to base or an allowed absolute path.")] string directoryPath,
        [Description("File pattern to filter (default: *.cs).")] string filePattern = "*.cs",
        [Description("Whether to search subdirectories.")] bool recursive = true) // Sync
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
    /// Reads and returns the contents of a specified file with encoding detection/specification.
    /// </summary>
    /// <param name=\"filePath\">Absolute path to the file to read.</param>
    /// <param name=\"startLine\">Optional start line (1-based).</param>
    /// <param name=\"endLine\">Optional end line (1-based).</param>
    /// <param name=\"forceEncoding\">Optional encoding to force. Options: Utf8NoBom, Utf8WithBom, Ascii, Utf16Le, Utf16Be, Utf32Le, SystemDefault, AutoDetect</param>
    /// <returns>The contents of the file with encoding information, or an error message if the file cannot be read.</returns>
    [McpServerTool("read_file_with_encoding")]
    [Description("Reads and returns the contents of a specified file with encoding detection/specification.")]
    public static async Task<string> ReadFileWithEncoding(
        [Description("Absolute path to the file to read")]
        string filePath,
        [Description("Optional start line (1-based) to read from.")]
        int? startLine = null,
        [Description("Optional end line (1-based) to read up to (inclusive).")]
        int? endLine = null,
        [Description("Optional encoding to force. Options: Utf8NoBom, Utf8WithBom, Ascii, Utf16Le, Utf16Be, Utf32Le, SystemDefault, AutoDetect")]
        string? forceEncoding = null)
    {
        try
        {
            var fileService = GetFileService();
            
            // Parse encoding if provided
            FileEncoding? encodingToUse = null;
            if (!string.IsNullOrEmpty(forceEncoding))
            {
                if (Enum.TryParse<FileEncoding>(forceEncoding, ignoreCase: true, out var parsedEncoding))
                {
                    encodingToUse = parsedEncoding;
                }
                else
                {
                    return JsonSerializer.Serialize(new { error = $"Invalid encoding '{forceEncoding}'. Valid options: Utf8NoBom, Utf8WithBom, Ascii, Utf16Le, Utf16Be, Utf32Le, SystemDefault, AutoDetect" }, DefaultJsonOptions);
                }
            }
            
            ReadFileResponse response = await fileService.ReadFileAsync(filePath, startLine, endLine, encodingToUse);
            return JsonSerializer.Serialize(response, DefaultJsonOptions);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = ex.Message }, DefaultJsonOptions);
        }
    }
}
