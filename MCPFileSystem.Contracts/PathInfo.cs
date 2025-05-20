namespace MCPFileSystem.Contracts;

public class PathInfo
{
    public string Path { get; set; } = string.Empty; // Added
    public bool Exists { get; set; }
    public string? Type { get; set; } // "file", "directory", "unknown"
    public bool IsDirectory { get; set; } // Added
    public bool IsFile { get; set; } // Added
    public bool IsReadOnly { get; set; } // Added
    public string? ErrorMessage { get; set; } // Added for consistency
}
