namespace MCPFileSystemServer.Models;

/// <summary>
/// Represents a text search match in a file.
/// </summary>
public class SearchMatch
{
    /// <summary>
    /// Gets or sets the line number where the match was found (1-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the text of the line containing the match.
    /// </summary>
    public string Text { get; set; } = string.Empty;
}
