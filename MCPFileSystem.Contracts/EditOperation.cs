namespace MCPFileSystem.Contracts;

public class EditOperation
{
    public string Type { get; set; } = string.Empty; // e.g., "INSERT", "DELETE", "REPLACE"
    public int StartLine { get; set; } // 0-based line number
    public int? EndLine { get; set; } // 0-based line number, inclusive for DELETE/REPLACE
    public string[] Content { get; set; } = Array.Empty<string>(); // Lines of text to insert/replace with

    // Retaining OldText and NewText for now, though FileService uses the properties above.
    // These might be useful for different editing scenarios or if the client sends them.
    public string OldText { get; set; } = string.Empty;
    public string NewText { get; set; } = string.Empty;
}
