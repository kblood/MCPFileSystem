using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MCPFileSystem.Client
{
    /// <summary>
    /// Client for interacting with the MCP Filesystem Server
    /// Uses Context7 patterns for clean, maintainable code
    /// </summary>
    public partial class MCPFileSystemClient
    {
        private readonly string _host;
        private readonly int _port;
        private readonly string _package = "filesystem-v1";

        public MCPFileSystemClient(string host, int port)
        {
            _host = host;
            _port = port;
        }

        private async Task<string> SendMCPRequestAsync(string command, string? data = null)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    // Set a reasonable timeout for connection attempts
                    client.SendTimeout = 5000;
                    client.ReceiveTimeout = 5000;
                    
                    Console.WriteLine($"Connecting to {_host}:{_port}...");
                    await client.ConnectAsync(_host, _port);
                    Console.WriteLine("Connected.");
                    
                    using (var stream = client.GetStream())
                    {
                        // Format MCP request
                        string request = $"{_package} {command}";
                        if (!string.IsNullOrEmpty(data))
                        {
                            request += $" {data}";
                        }
                        request += "\r\n";
                        
                        Console.WriteLine($"Sending request: {request.TrimEnd('\r', '\n')}");
                        
                        // Send request
                        byte[] requestBytes = Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(requestBytes, 0, requestBytes.Length);
                        await stream.FlushAsync();
                        
                        Console.WriteLine("Request sent, waiting for response...");
                        
                        // Read response
                        var buffer = new byte[8192]; // Larger buffer for file contents
                        var message = new StringBuilder();
                        int bytesRead;
                        var readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                        
                        // Set a timeout for the read operation
                        if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                        {
                            throw new TimeoutException("Timeout waiting for server response");
                        }
                        
                        bytesRead = await readTask;
                        if (bytesRead <= 0)
                        {
                            throw new IOException("Server closed connection or sent empty response");
                        }
                        
                        message.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        
                        // Check if we've received a complete MCP response
                        while (!message.ToString().EndsWith("\r\n"))
                        {
                            readTask = stream.ReadAsync(buffer, 0, buffer.Length);
                            
                            if (await Task.WhenAny(readTask, Task.Delay(5000)) != readTask)
                            {
                                throw new TimeoutException("Timeout waiting for complete server response");
                            }
                            
                            bytesRead = await readTask;
                            if (bytesRead <= 0) break; // End of stream
                            
                            message.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                        }
                        
                        var response = message.ToString().TrimEnd('\r', '\n');
                        Console.WriteLine($"Received response: {response}");
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR in SendMCPRequestAsync: {ex.GetType().Name}: {ex.Message}");
                throw; // Re-throw the exception for higher level handling
            }
        }

        private MCPResponse ParseResponse(string response)
        {
            if (string.IsNullOrEmpty(response))
            {
                return new MCPResponse
                {
                    IsSuccess = false,
                    Data = "Empty response from server"
                };
            }
            
            var parts = response.Split(' ', 2);
            string status = parts[0];
            string? data = parts.Length > 1 ? parts[1] : string.Empty;
            
            Console.WriteLine($"Parsed response - Status: {status}, Data: {(data?.Length > 100 ? data.Substring(0, 100) + "..." : data)}");
            
            return new MCPResponse
            {
                IsSuccess = status == "ok",
                Data = data ?? string.Empty
            };
        }

        public async Task<List<FileSystemEntry>> ListDirectoryAsync(string path, bool respectGitignore = true)
        {
            try
            {
                Console.WriteLine($"ListDirectoryAsync: {path}, respectGitignore: {respectGitignore}");
                var parameters = JsonSerializer.Serialize(new { path, respectGitignore });
                var response = await SendMCPRequestAsync("list", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to list directory: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for ListDirectoryAsync");
                    return new List<FileSystemEntry>(); // Return empty list instead of failing
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<List<FileSystemEntry>>(parsedResponse.Data, options) ?? new List<FileSystemEntry>();
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
                Console.WriteLine($"ERROR in ListDirectoryAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        public async Task<string> ReadFileAsync(string path)
        {
            try
            {
                Console.WriteLine($"ReadFileAsync: {path}");
                var parameters = JsonSerializer.Serialize(new { path });
                var response = await SendMCPRequestAsync("read", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to read file: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for ReadFileAsync");
                    return string.Empty;
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    var result = JsonSerializer.Deserialize<ReadFileResponse>(parsedResponse.Data, options);
                    if (result == null)
                    {
                        return string.Empty;
                    }
                    
                    // Handle different encodings
                    if (result.Encoding == "base64" && !string.IsNullOrEmpty(result.Content))
                    {
                        Console.WriteLine("Decoding base64 content...");
                        byte[] bytes = Convert.FromBase64String(result.Content);
                        return Encoding.UTF8.GetString(bytes);
                    }
                    
                    return result.Content ?? string.Empty;
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
                Console.WriteLine($"ERROR in ReadFileAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        public async Task WriteFileAsync(string path, string content, string encoding = "utf8")
        {
            var parameters = JsonSerializer.Serialize(new
            {
                path,
                content,
                encoding
            });
            
            var response = await SendMCPRequestAsync("write", parameters);
            var parsedResponse = ParseResponse(response);
            
            if (!parsedResponse.IsSuccess)
            {
                throw new Exception($"Failed to write file: {parsedResponse.Data}");
            }
        }

        public async Task DeleteFileAsync(string path)
        {
            var parameters = JsonSerializer.Serialize(new { path });
            var response = await SendMCPRequestAsync("delete", parameters);
            var parsedResponse = ParseResponse(response);
            
            if (!parsedResponse.IsSuccess)
            {
                throw new Exception($"Failed to delete file: {parsedResponse.Data}");
            }
        }

        public async Task CreateDirectoryAsync(string path)
        {
            var parameters = JsonSerializer.Serialize(new { path });
            var response = await SendMCPRequestAsync("mkdir", parameters);
            var parsedResponse = ParseResponse(response);
            
            if (!parsedResponse.IsSuccess)
            {
                throw new Exception($"Failed to create directory: {parsedResponse.Data}");
            }
        }

        public async Task DeleteDirectoryAsync(string path, bool recursive = false)
        {
            var parameters = JsonSerializer.Serialize(new
            {
                path,
                recursive = recursive.ToString().ToLower()
            });
            
            var response = await SendMCPRequestAsync("rmdir", parameters);
            var parsedResponse = ParseResponse(response);
            
            if (!parsedResponse.IsSuccess)
            {
                throw new Exception($"Failed to delete directory: {parsedResponse.Data}");
            }
        }

        public async Task<PathInfo> CheckExistsAsync(string path)
        {
            try
            {
                Console.WriteLine($"CheckExistsAsync: {path}");
                var parameters = JsonSerializer.Serialize(new { path });
                var response = await SendMCPRequestAsync("exists", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to check if path exists: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for CheckExistsAsync");
                    return new PathInfo { Exists = false, Type = "none" };
                }
                
                try 
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<PathInfo>(parsedResponse.Data, options) 
                        ?? new PathInfo { Exists = false, Type = "none" };
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
                Console.WriteLine($"ERROR in CheckExistsAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }

        public async Task<FileSystemInfo> GetFileInfoAsync(string path)
        {
            try
            {
                Console.WriteLine($"GetFileInfoAsync: {path}");
                var parameters = JsonSerializer.Serialize(new { path });
                var response = await SendMCPRequestAsync("info", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to get file info: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for GetFileInfoAsync");
                    return new FileSystemInfo(); // Return empty object instead of failing
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<FileSystemInfo>(parsedResponse.Data, options) 
                        ?? new FileSystemInfo();
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
                Console.WriteLine($"ERROR in GetFileInfoAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Gets a hierarchical tree representation of a directory and its contents
        /// </summary>
        /// <param name="path">Path to get the directory tree for</param>
        /// <param name="respectGitignore">Whether to respect .gitignore rules</param>
        /// <param name="showHidden">Whether to include hidden files and directories</param>
        /// <param name="includeGitFolders">Whether to include .git folders</param>
        /// <returns>A tree structure representing the directory</returns>
        public async Task<DirectoryTreeNode> GetDirectoryTreeAsync(string path, bool respectGitignore = true, bool showHidden = false, bool includeGitFolders = false)
        {
            try
            {
                Console.WriteLine($"GetDirectoryTreeAsync: {path}, respectGitignore: {respectGitignore}, showHidden: {showHidden}, includeGitFolders: {includeGitFolders}");
                var parameters = JsonSerializer.Serialize(new { path, respectGitignore, showHidden, includeGitFolders });
                var response = await SendMCPRequestAsync("tree", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to get directory tree: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for GetDirectoryTreeAsync");
                    // Return a root node with the requested path as name
                    return new DirectoryTreeNode 
                    { 
                        Name = Path.GetFileName(path) ?? path,
                        Type = "directory"
                    };
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<DirectoryTreeNode>(parsedResponse.Data, options) 
                        ?? new DirectoryTreeNode 
                        { 
                            Name = Path.GetFileName(path) ?? path,
                            Type = "directory"
                        };
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
        
        /// <summary>
        /// Searches for files matching the given pattern
        /// </summary>
        /// <param name="path">Path to search in</param>
        /// <param name="pattern">Search pattern</param>
        /// <returns>List of matching search results</returns>
        public async Task<List<SearchResult>> SearchFilesAsync(string path, string pattern)
        {
            try
            {
                Console.WriteLine($"SearchFilesAsync: {path}, pattern: {pattern}");
                var parameters = JsonSerializer.Serialize(new { path, pattern });
                var response = await SendMCPRequestAsync("search", parameters);
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
        
        /// <summary>
        /// Moves or renames a file or directory
        /// </summary>
        /// <param name="source">Source path</param>
        /// <param name="destination">Destination path</param>
        public async Task MoveFileAsync(string source, string destination)
        {
            try
            {
                Console.WriteLine($"MoveFileAsync: {source} to {destination}");
                var parameters = JsonSerializer.Serialize(new { source, destination });
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
        
        /// <summary>
        /// Edits a file by applying a series of text replacements
        /// </summary>
        /// <param name="path">Path to the file to edit</param>
        /// <param name="operations">List of edit operations to perform</param>
        /// <returns>Result of the edit operation</returns>
        public async Task<EditResult> EditFileAsync(string path, List<EditOperation> operations)
        {
            try
            {
                Console.WriteLine($"EditFileAsync: {path}, {operations.Count} operations");
                var parameters = JsonSerializer.Serialize(new { path, operations });
                var response = await SendMCPRequestAsync("edit", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to edit file: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for EditFileAsync");
                    return new EditResult { EditCount = 0, Diff = "No changes" };
                }
                
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    
                    return JsonSerializer.Deserialize<EditResult>(parsedResponse.Data, options) 
                        ?? new EditResult { EditCount = 0, Diff = "Failed to parse response" };
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
        
        /// <summary>
        /// Lists all directories accessible by the server
        /// </summary>
        /// <returns>List of accessible directories</returns>
        public async Task<List<DirectoryInfo>> ListAccessibleDirectoriesAsync()
        {
            try
            {
                Console.WriteLine("ListAccessibleDirectoriesAsync");
                var response = await SendMCPRequestAsync("dirs");
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to list directories: {parsedResponse.Data}");
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
                    
                    return JsonSerializer.Deserialize<List<DirectoryInfo>>(parsedResponse.Data, options) 
                        ?? new List<DirectoryInfo>();
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
    }

    public class MCPResponse
    {
        public bool IsSuccess { get; set; }
        public string Data { get; set; } = string.Empty;
    }

    public class FileSystemEntry
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public long? Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime? Modified { get; set; }
        
        public override string ToString()
        {
            return $"Name: {Name}, Type: {Type}, Size: {Size}, Created: {Created}, Modified: {Modified}";
        }
    }

    public class ReadFileResponse
    {
        public string? Content { get; set; }
        public string? Encoding { get; set; }
    }

    public class PathInfo
    {
        public bool Exists { get; set; }
        public string? Type { get; set; }
    }

    public class FileSystemInfo
    {
        public string? Name { get; set; }
        public string? Path { get; set; }
        public long? Size { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string? Attributes { get; set; }
    }

    public class DirectoryTreeNode
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public long? Size { get; set; }
        public List<DirectoryTreeNode> Children { get; set; }
        
        public DirectoryTreeNode()
        {
            Children = new List<DirectoryTreeNode>();
        }
    }

    public class EditOperation
    {
        public string OldText { get; set; } = string.Empty;
        public string NewText { get; set; } = string.Empty;
    }

    public class EditResult
    {
        public int EditCount { get; set; }
        public string Diff { get; set; } = string.Empty;
    }

    public class SearchResult
    {
        public string Path { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
    }

    class Program
    {
        static async Task Main(string[] args)
        {
            string host = "localhost";
            int port = 8080;
            
            // Parse command line arguments
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--host" && i + 1 < args.Length)
                {
                    host = args[i + 1];
                    i++;
                }
                else if (args[i] == "--port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int p))
                    {
                        port = p;
                    }
                    i++;
                }
            }

            var client = new MCPFileSystemClient(host, port);
            
            Console.WriteLine($"MCP File System Client - Connected to {host}:{port}");
            Console.WriteLine("Type 'help' for available commands or 'exit' to quit");
            
            bool running = true;
            while (running)
            {
                Console.Write("> ");
                string? input = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(input))
                    continue;
                
                string[] cmdParts = input.Split(' ', 2);
                string cmd = cmdParts[0].ToLower();
                string arg = cmdParts.Length > 1 ? cmdParts[1] : string.Empty;
                
                try
                {
                    switch (cmd)
                    {
                        case "exit":
                            running = false;
                            break;
                        
                        case "help":
                            ShowHelp();
                            break;
                        
                        case "ls":
                            bool respectGitignore = true;
                            string path = arg;
                            
                            // Check if we have the --no-ignore flag
                            if (path.Contains("--no-ignore"))
                            {
                                respectGitignore = false;
                                path = path.Replace("--no-ignore", "").Trim();
                            }
                            
                            var entries = await client.ListDirectoryAsync(path, respectGitignore);
                            Console.WriteLine($"Directory listing for: {path}" + (respectGitignore ? " (respecting .gitignore)" : " (ignoring .gitignore)"));
                            Console.WriteLine("--------------------------------------------------");
                            foreach (var entry in entries)
                            {
                                if (entry.Type == "directory")
                                {
                                    Console.WriteLine($"[DIR] {entry.Name} (Created: {entry.Created})");
                                }
                                else
                                {
                                    Console.WriteLine($"[FILE] {entry.Name} ({entry.Size} bytes, Modified: {entry.Modified})");
                                }
                            }
                            break;
                        
                        case "read":
                            var content = await client.ReadFileAsync(arg);
                            Console.WriteLine($"Content of {arg}:");
                            Console.WriteLine("--------------------------------------------------");
                            Console.WriteLine(content);
                            break;
                        
                        case "write":
                            Console.WriteLine("Enter file content (press Ctrl+Z on a new line when done):");
                            var sb = new StringBuilder();
                            string? line;
                            while ((line = Console.ReadLine()) != null)
                            {
                                sb.AppendLine(line);
                            }
                            
                            await client.WriteFileAsync(arg, sb.ToString());
                            Console.WriteLine($"File {arg} written successfully");
                            break;
                        
                        case "rm":
                            await client.DeleteFileAsync(arg);
                            Console.WriteLine($"File {arg} deleted successfully");
                            break;
                        
                        case "mkdir":
                            await client.CreateDirectoryAsync(arg);
                            Console.WriteLine($"Directory {arg} created successfully");
                            break;
                        
                        case "rmdir":
                            await client.DeleteDirectoryAsync(arg);
                            Console.WriteLine($"Directory {arg} deleted successfully");
                            break;
                        
                        case "exists":
                            var pathInfo = await client.CheckExistsAsync(arg);
                            Console.WriteLine($"Path {arg} exists: {pathInfo.Exists} (Type: {pathInfo.Type})");
                            break;
                        
                        case "info":
                            var fileInfo = await client.GetFileInfoAsync(arg);
                            Console.WriteLine($"Information for {arg}:");
                            Console.WriteLine($"Name: {fileInfo.Name}");
                            Console.WriteLine($"Path: {fileInfo.Path}");
                            if (fileInfo.Size.HasValue)
                            {
                                Console.WriteLine($"Size: {fileInfo.Size} bytes");
                            }
                            Console.WriteLine($"Created: {fileInfo.Created}");
                            Console.WriteLine($"Modified: {fileInfo.Modified}");
                            Console.WriteLine($"Attributes: {fileInfo.Attributes}");
                            break;
                            
                        case "tree":
                            bool treeRespectGitignore = true;
                            bool showHidden = false;
                            bool includeGitFolders = false;
                            string treePath = arg;
                            
                            // Process command line flags
                            if (treePath.Contains("--no-ignore"))
                            {
                                treeRespectGitignore = false;
                                treePath = treePath.Replace("--no-ignore", "").Trim();
                            }
                            
                            if (treePath.Contains("--show-hidden"))
                            {
                                showHidden = true;
                                treePath = treePath.Replace("--show-hidden", "").Trim();
                            }
                            
                            if (treePath.Contains("--show-git"))
                            {
                                includeGitFolders = true;
                                treePath = treePath.Replace("--show-git", "").Trim();
                            }
                            
                            var tree = await client.GetDirectoryTreeAsync(treePath, treeRespectGitignore, showHidden, includeGitFolders);
                            Console.WriteLine($"Directory tree for {treePath}:");
                            if (!treeRespectGitignore) Console.WriteLine("  (ignoring .gitignore)");
                            if (showHidden) Console.WriteLine("  (showing hidden files)");
                            if (includeGitFolders) Console.WriteLine("  (showing .git folders)");
                            
                            PrintDirectoryTree(tree, 0);
                            break;
                            
                        case "search":
                            var parts = arg.Split(' ', 2);
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: search <path> <pattern>");
                                break;
                            }
                            
                            var searchPath = parts[0];
                            var pattern = parts[1];
                            var results = await client.SearchFilesAsync(searchPath, pattern);
                            
                            Console.WriteLine($"Search results for pattern '{pattern}' in {searchPath}:");
                            foreach (var result in results)
                            {
                                Console.WriteLine($"  {result.Path} ({result.Type})");
                            }
                            break;
                            
                        case "move":
                            parts = arg.Split(' ', 2);
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: move <source> <destination>");
                                break;
                            }
                            
                            var source = parts[0];
                            var destination = parts[1];
                            await client.MoveFileAsync(source, destination);
                            Console.WriteLine($"Moved {source} to {destination}");
                            break;
                            
                        case "edit":
                            parts = arg.Split(' ', 2);
                            if (parts.Length < 2)
                            {
                                Console.WriteLine("Usage: edit <path> <oldText>:<newText>");
                                break;
                            }
                            
                            var editPath = parts[0];
                            var editParts = parts[1].Split(':', 2);
                            
                            if (editParts.Length < 2)
                            {
                                Console.WriteLine("Usage: edit <path> <oldText>:<newText>");
                                break;
                            }
                            
                            var oldText = editParts[0];
                            var newText = editParts[1];
                            
                            var editResult = await client.EditFileAsync(editPath, new List<EditOperation>
                            {
                                new EditOperation { OldText = oldText, NewText = newText }
                            });
                            
                            Console.WriteLine($"Edit result: {editResult.EditCount} edits applied");
                            Console.WriteLine("Diff:\n" + editResult.Diff);
                            break;
                            
                        case "dirs":
                            var dirs = await client.ListAccessibleDirectoriesAsync();
                            Console.WriteLine("Accessible directories:");
                            foreach (var dir in dirs)
                            {
                                Console.WriteLine($"  {dir.Name} (Alias: {dir.Alias}, Path: {dir.Path})");
                            }
                            break;
                        
                        default:
                            Console.WriteLine($"Unknown command: {cmd}");
                            ShowHelp();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                
                Console.WriteLine();
            }
        }

        static void ShowHelp()
        {
            Console.WriteLine("Available commands:");
            Console.WriteLine("  help                 - Show this help");
            Console.WriteLine("  exit                 - Exit the client");
            Console.WriteLine("  ls [path]            - List directory contents");
            Console.WriteLine("  read <path>          - Read file content");
            Console.WriteLine("  write <path>         - Write to file (press Ctrl+Z to finish input)");
            Console.WriteLine("  rm <path>            - Delete a file");
            Console.WriteLine("  mkdir <path>         - Create a directory");
            Console.WriteLine("  rmdir <path>         - Delete a directory");
            Console.WriteLine("  exists <path>        - Check if a path exists");
            Console.WriteLine("  info <path>          - Get file or directory information");
            Console.WriteLine("  tree [path] [flags]  - Show directory tree");
            Console.WriteLine("    Flags: --no-ignore    - Don't respect .gitignore");
            Console.WriteLine("           --show-hidden  - Show hidden files");
            Console.WriteLine("           --show-git     - Show .git folders");
            Console.WriteLine("  search <path> <pat>  - Search for files matching pattern");
            Console.WriteLine("  move <src> <dst>     - Move/rename file or directory");
            Console.WriteLine("  edit <path> <o>:<n>  - Edit file (replace old:new text)");
            Console.WriteLine("  dirs                 - List accessible directories");
        }
        
        static void PrintDirectoryTree(DirectoryTreeNode node, int level)
        {
            string indent = new string(' ', level * 4);
            string prefix = node.Type == "directory" ? "[DIR] " : "[FILE] ";
            
            Console.WriteLine($"{indent}{prefix}{node.Name}");
            
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    PrintDirectoryTree(child, level + 1);
                }
            }
        }
    }
}
