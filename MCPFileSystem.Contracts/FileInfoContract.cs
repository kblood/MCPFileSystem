namespace MCPFileSystem.Contracts;

// Renamed to avoid conflict with System.IO.FileInfo
public class FileInfoContract
{
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty; // Was Path
    public long Length { get; set; } // Was Size
    public DateTime CreationTime { get; set; } // Was Created
    public DateTime LastWriteTime { get; set; } // Was Modified
    public DateTime LastAccessTime { get; set; } // Added
    public bool IsReadOnly { get; set; } // Was string Attributes, now bool
    public string Extension { get; set; } = string.Empty;
    public string? DirectoryName { get; set; }
    public bool Exists { get; set; } // Added
    public string? ErrorMessage { get; set; } // For returning errors
}
