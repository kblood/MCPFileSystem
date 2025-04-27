namespace MCPFileSystemServer.Models;

/// <summary>
/// Represents metadata about a file.
/// </summary>
public class FileInfo
{
    /// <summary>
    /// Gets or sets the name of the file.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the full path of the file.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the size of the file in bytes.
    /// </summary>
    public long Size { get; set; }
    
    /// <summary>
    /// Gets or sets the creation time of the file.
    /// </summary>
    public DateTime Created { get; set; }
    
    /// <summary>
    /// Gets or sets the last modified time of the file.
    /// </summary>
    public DateTime Modified { get; set; }
    
    /// <summary>
    /// Gets or sets the file attributes.
    /// </summary>
    public string Attributes { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the file extension.
    /// </summary>
    public string Extension { get; set; } = string.Empty;
}
