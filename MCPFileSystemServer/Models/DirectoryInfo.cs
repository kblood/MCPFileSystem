namespace MCPFileSystemServer.Models;

/// <summary>
/// Represents metadata about a directory.
/// </summary>
public class DirectoryInfo
{
    /// <summary>
    /// Gets or sets the name of the directory.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the full path of the directory.
    /// </summary>
    public string Path { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the creation time of the directory.
    /// </summary>
    public DateTime Created { get; set; }
    
    /// <summary>
    /// Gets or sets the last modified time of the directory.
    /// </summary>
    public DateTime Modified { get; set; }
    
    /// <summary>
    /// Gets or sets the directory attributes.
    /// </summary>
    public string Attributes { get; set; } = string.Empty;
    
    /// <summary>
    /// Gets or sets the number of files in the directory.
    /// </summary>
    public int FileCount { get; set; }
    
    /// <summary>
    /// Gets or sets the number of subdirectories in the directory.
    /// </summary>
    public int DirectoryCount { get; set; }
}
