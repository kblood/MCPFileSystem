using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCPFileSystem.Client
{
    // Extension methods for the MCPFileSystemClient class to add new features
    public partial class MCPFileSystemClient
    {
        // This method is replaced by the one in Program.cs that includes respectGitignore parameter
        /*
        public async Task<DirectoryTreeNode> GetDirectoryTreeAsync(string path)
        {
            // Implementation removed as it's now in Program.cs with additional parameter
        }
        */
        
        public async Task<List<SearchResult>> SearchFilesAsync(string path, string pattern, List<string>? excludePatterns = null)
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
        
        // Removed duplicate MoveFileAsync implementation
        
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
                    return new EditResult { EditCount = 0, Diff = string.Empty };
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<EditResult>(parsedResponse.Data, options) ?? new EditResult { EditCount = 0, Diff = string.Empty };
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
    
    // Model class for the new features
    public class DirectoryInfo
    {
        public string Alias { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
    }
}
