namespace MCPFileSystem.Contracts;

// Renamed to avoid conflict with System.IO.DirectoryInfo
public class DirectoryInfoContract
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // Was Path
    public DateTime CreationTime { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime LastAccessTime { get; set; } // Added
    public string? ParentDirectory { get; set; }
    public string? RootDirectory { get; set; }
    public bool Exists { get; set; } // Added
    public string? ErrorMessage { get; set; } // For returning errors

    // Keeping Alias for now if it\'s used elsewhere, otherwise can be removed.
    public string Alias { get; set; } = string.Empty;
}
