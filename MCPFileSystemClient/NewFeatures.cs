using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCPFileSystem.Client
{
    // Extension methods for the MCPFileSystemClient class to add new features
    public partial class MCPFileSystemClient
    {
        public async Task<List<DirectoryInfo>> ListAccessibleDirectoriesAsync()
        {
            try
            {
                Console.WriteLine("ListAccessibleDirectoriesAsync");
                var response = await SendMCPRequestAsync("list_accessible");
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to list accessible directories: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for ListAccessibleDirectoriesAsync");
                    return new List<DirectoryInfo>();
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<List<DirectoryInfo>>(parsedResponse.Data, options) ?? new List<DirectoryInfo>();
                }
                catch (JsonException je)
                {
                    Console.WriteLine($"JSON Error: {je.Message}");
                    Console.WriteLine($"JSON Data: {parsedResponse.Data}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in ListAccessibleDirectoriesAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<DirectoryTreeNode> GetDirectoryTreeAsync(string path)
        {
            try
            {
                Console.WriteLine($"GetDirectoryTreeAsync: {path}");
                var response = await SendMCPRequestAsync("tree", path);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to get directory tree: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for GetDirectoryTreeAsync");
                    return new DirectoryTreeNode { Name = "Empty", Type = "directory", Children = new List<DirectoryTreeNode>() };
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<DirectoryTreeNode>(parsedResponse.Data, options);
                }
                catch (JsonException je)
                {
                    Console.WriteLine($"JSON Error: {je.Message}");
                    Console.WriteLine($"JSON Data: {parsedResponse.Data}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in GetDirectoryTreeAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<List<SearchResult>> SearchFilesAsync(string path, string pattern, List<string> excludePatterns = null)
        {
            try
            {
                Console.WriteLine($"SearchFilesAsync: {path}, Pattern: {pattern}");
                
                var parameters = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["pattern"] = pattern
                };
                
                if (excludePatterns != null && excludePatterns.Count > 0)
                {
                    parameters["excludePatterns"] = JsonSerializer.Serialize(excludePatterns);
                }
                
                var response = await SendMCPRequestAsync("search", JsonSerializer.Serialize(parameters));
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to search files: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for SearchFilesAsync");
                    return new List<SearchResult>();
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<List<SearchResult>>(parsedResponse.Data, options) ?? new List<SearchResult>();
                }
                catch (JsonException je)
                {
                    Console.WriteLine($"JSON Error: {je.Message}");
                    Console.WriteLine($"JSON Data: {parsedResponse.Data}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in SearchFilesAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        public async Task MoveFileAsync(string source, string destination)
        {
            try
            {
                Console.WriteLine($"MoveFileAsync: From {source} to {destination}");
                var parameters = JsonSerializer.Serialize(new
                {
                    source,
                    destination
                });
                
                var response = await SendMCPRequestAsync("move", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to move file: {parsedResponse.Data}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in MoveFileAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        public async Task<EditResult> EditFileAsync(string path, List<EditOperation> edits, bool dryRun = false)
        {
            try
            {
                Console.WriteLine($"EditFileAsync: {path}, Edits: {edits.Count}, DryRun: {dryRun}");
                
                var parameters = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["edits"] = edits,
                    ["dryRun"] = dryRun
                };
                
                var response = await SendMCPRequestAsync("edit", JsonSerializer.Serialize(parameters));
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to edit file: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for EditFileAsync");
                    return new EditResult { EditCount = 0, Diff = "" };
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<EditResult>(parsedResponse.Data, options) ?? new EditResult { EditCount = 0, Diff = "" };
                }
                catch (JsonException je)
                {
                    Console.WriteLine($"JSON Error: {je.Message}");
                    Console.WriteLine($"JSON Data: {parsedResponse.Data}");
                    throw;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in EditFileAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
    }
    
    // Model classes for the new features
    public class DirectoryInfo
    {
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }
    
    public class DirectoryTreeNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public long? Size { get; set; }
        public List<DirectoryTreeNode> Children { get; set; }
    }
    
    public class SearchResult
    {
        public string Path { get; set; }
        public string Type { get; set; }
    }
    
    public class EditOperation
    {
        public string OldText { get; set; }
        public string NewText { get; set; }
    }
    
    public class EditResult
    {
        public int EditCount { get; set; }
        public string Diff { get; set; }
    }
}
