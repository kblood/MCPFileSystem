namespace MCPFileSystem.Contracts;

public class ReadFileResponse
{
    public string FilePath { get; set; } = string.Empty; // Ensured non-nullable
    public string[] Lines { get; set; } = Array.Empty<string>(); // Ensured non-nullable
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int TotalLines { get; set; }
    public string? FileSHA { get; set; } // Added
    public string? ErrorMessage { get; set; }
    public string? Content { get; set; } 
    public string? Encoding { get; set; } 
}
