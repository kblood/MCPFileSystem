using System.Text.RegularExpressions;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Provides validation for file operations, ensuring access control and safety.
/// </summary>
public static class FileValidationService
{
    private static string _baseDirectory = Directory.GetCurrentDirectory();
    private static readonly List<string> _accessibleDirectories = new();
    private static readonly object _syncLock = new object(); // Add thread safety

    static FileValidationService()
    {
        // Add base directory as the default accessible directory
        _accessibleDirectories.Add(_baseDirectory);
    }

    /// <summary>
    /// Gets the current base directory used for file operations.
    /// </summary>
    public static string BaseDirectory => _baseDirectory;

    /// <summary>
    /// Gets all directories that are accessible for file operations.
    /// </summary>
    public static IReadOnlyList<string> AllowedBaseDirectories => _accessibleDirectories.AsReadOnly();

    /// <summary>
    /// Sets the base directory for file operations.
    /// </summary>
    /// <param name="directory">The directory to set as the base directory.</param>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    public static void SetBaseDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        lock (_syncLock)
        {
            var normalizedPath = Path.GetFullPath(directory);
            _baseDirectory = normalizedPath;
            
            // Make sure the base directory is always the first item in the accessible list
            // First remove it if it's already in the list (at any position)
            _accessibleDirectories.RemoveAll(d => 
                string.Equals(Path.GetFullPath(d), normalizedPath, StringComparison.OrdinalIgnoreCase));
            
            // Then add it as the first item
            _accessibleDirectories.Insert(0, normalizedPath);
        }
    }

    /// <summary>
    /// Adds a directory to the list of accessible directories.
    /// </summary>
    /// <param name="directory">The directory to add.</param>
    /// <returns>True if the directory was added, false if it was already in the list.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown if the directory does not exist.</exception>
    public static bool AddAccessibleDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Directory not found: {directory}");
        }

        lock (_syncLock)
        {
            var normalizedPath = Path.GetFullPath(directory);
            
            // Check if this is the same as the base directory
            if (string.Equals(normalizedPath, Path.GetFullPath(_baseDirectory), StringComparison.OrdinalIgnoreCase))
            {
                return false; // It's already the base directory, which is always accessible
            }
            
            // Check if it's already in the list
            if (_accessibleDirectories.Any(d => 
                string.Equals(Path.GetFullPath(d), normalizedPath, StringComparison.OrdinalIgnoreCase)))
            {
                return false; // Already in the list
            }

            _accessibleDirectories.Add(normalizedPath);
            return true;
        }
    }

    /// <summary>
    /// Validates if a path is within any of the accessible directories.
    /// </summary>
    /// <param name="path">The path to validate.</param>
    /// <returns>True if the path is safe to access, false otherwise.</returns>
    public static bool IsPathSafe(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return false;
        }

        try
        {
            // Special case for the base directory and "." notation
            if (path == "." || path == _baseDirectory)
            {
                return true;
            }

            var fullPath = Path.GetFullPath(path);
            
            // Check if the path is exactly one of the accessible directories
            if (_accessibleDirectories.Any(dir => string.Equals(fullPath, Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
            
            // Check if the path is within any of the accessible directories
            return _accessibleDirectories.Any(dir => 
                fullPath.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Normalizes a path to ensure it's within an accessible directory.
    /// </summary>
    /// <param name="path">The path to normalize.</param>
    /// <returns>A normalized absolute path.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown if the path is not within an accessible directory.</exception>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            // Default to the base directory if path is not specified
            return _baseDirectory;
        }

        // Direct handling for "." notation to reference the base directory
        if (path == "." || path == "./" || path == ".\\" || path == ".:")
        {
            return _baseDirectory;
        }

        // Handle paths starting with "./" or ".\\"
        if (path.StartsWith("./") || path.StartsWith(".\\"))
        {
            string relativePath = path.Substring(2);
            string fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, relativePath));
            
            if (IsPathSafe(fullPath))
            {
                return fullPath;
            }
        }

        // Directory alias format: "dir1:/file.txt"
        if (path.Contains(':'))
        {
            var parts = path.Split(new[] { ':' }, 2);
            var alias = parts[0].ToLower();
            var relativePath = parts[1];

            // Remove leading slash if present
            if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
            {
                relativePath = relativePath.Substring(1);
            }

            // Special case for "." alias to reference the base directory
            if (alias == ".")
            {
                var fullPath = Path.GetFullPath(Path.Combine(_baseDirectory, relativePath));
                if (IsPathSafe(fullPath))
                {
                    return fullPath;
                }
            }

            // Find the directory with the matching alias
            for (int i = 0; i < _accessibleDirectories.Count; i++)
            {
                var dirName = Path.GetFileName(_accessibleDirectories[i]);
                if (dirName.ToLower() == alias || $"dir{i + 1}" == alias)
                {
                    var fullPath = Path.GetFullPath(Path.Combine(_accessibleDirectories[i], relativePath));
                    if (IsPathSafe(fullPath))
                    {
                        return fullPath;
                    }
                }
            }
        }
        
        // Check if this is an absolute path
        var absolutePath = Path.IsPathRooted(path) ? path : Path.Combine(_baseDirectory, path);
        var normalizedPath = Path.GetFullPath(absolutePath);

        if (IsPathSafe(normalizedPath))
        {
            return normalizedPath;
        }

        throw new UnauthorizedAccessException($"Access to the path '{path}' is denied");
    }
}
