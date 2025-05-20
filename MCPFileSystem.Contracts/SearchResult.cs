namespace MCPFileSystem.Contracts;

public class SearchResult
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public List<SearchMatch> Matches { get; set; } = new List<SearchMatch>();
    public string ErrorMessage { get; set; } = string.Empty;
    public bool IsBinaryFile { get; set; }
}
