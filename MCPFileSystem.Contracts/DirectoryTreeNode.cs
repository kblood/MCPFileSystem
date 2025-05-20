namespace MCPFileSystem.Contracts;

public class DirectoryTreeNode
{
    public string Name { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty; // Added
    public string Type { get; set; } = string.Empty; // "Directory", "File", or "Error"
    public long? Size { get; set; } // Optional: for files
    public List<DirectoryTreeNode> Children { get; set; } = new List<DirectoryTreeNode>();
    // public string? ErrorMessage { get; set; } // Optional: if node represents an error
}
