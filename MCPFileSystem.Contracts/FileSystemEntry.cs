namespace MCPFileSystem.Contracts;

public class FileSystemEntry
{
    public string Name { get; set; } = string.Empty; // Made non-nullable
    public string Path { get; set; } = string.Empty; // Added
    public bool IsDirectory { get; set; } // Added
    public string Type { get; set; } = string.Empty; // "file" or "directory", made non-nullable
    public long Size { get; set; } // Made non-nullable, was long?
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; } // Renamed from Modified, made non-nullable

    public override string ToString()
    {
        return $"Name: {Name}, Path: {Path}, Type: {Type}, IsDirectory: {IsDirectory}, Size: {Size}, Created: {Created}, Modified: {LastModified}";
    }
}
