namespace MCPFileSystem.Contracts;

public class EditResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? NewFileSHA { get; set; } // Added

    // Retaining existing properties for now.
    public int EditCount { get; set; }
    public string Diff { get; set; } = string.Empty;
    public string? PreservedEncoding { get; set; } // Added for encoding preservation tracking
}
