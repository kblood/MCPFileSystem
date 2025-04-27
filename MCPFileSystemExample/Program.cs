using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MCPFileSystem.Client;
using MCPFileSystem.Example;
namespace MCPFileSystem.Example
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("MCP Filesystem Example - Context7");
            Console.WriteLine("==================================");

            // Start the server process
            Console.WriteLine("Starting MCP Filesystem Server...");
            // Create test directories for the server to use
            string testDir1 = Path.Combine(Directory.GetCurrentDirectory(), "test_files");
            string testDir2 = Path.Combine(Directory.GetCurrentDirectory(), "additional_files");
            
            if (!Directory.Exists(testDir1))
            {
                Directory.CreateDirectory(testDir1);
            }
            
            if (!Directory.Exists(testDir2))
            {
                Directory.CreateDirectory(testDir2);
            }
            
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"run --project ..\\MCPFileSystemServer\\MCPFileSystemServer.csproj -- --port 8080 --root \"{testDir1}\" --dir \"{testDir2}\"",
                UseShellExecute = false,
                CreateNoWindow = false,
            };
            
            Process serverProcess = new Process { StartInfo = startInfo };
            try
            {
                serverProcess.Start();
                
                // Wait a moment for the server to start
                Console.WriteLine("Waiting for server to initialize...");
                await Task.Delay(5000);
                
                // Create client instance connecting to the local MCP server
                Console.WriteLine("Initializing client connection...");
                var client = new MCPFileSystemClient("localhost", 8080);

                
                // 1. Create a directory
                Console.WriteLine("1. Creating directory 'examples'...");
                await client.CreateDirectoryAsync("examples");
                
                // 2. Write a text file
                Console.WriteLine("2. Writing text file...");
                string content = "This is a test file created using the MCP Filesystem.\n" +
                                "It demonstrates how to use the Context7 MCP protocol for filesystem operations.\n" +
                                "This is a simple example of working with files and directories.";
                
                await client.WriteFileAsync("examples/test.txt", content);
                
                // 3. List directory contents
                Console.WriteLine("3. Listing directory contents...");
                var entries = await client.ListDirectoryAsync("examples");
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
                
                // 4. Read file back
                Console.WriteLine("\n4. Reading file content...");
                string readContent = await client.ReadFileAsync("examples/test.txt");
                Console.WriteLine("File content:");
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine(readContent);
                Console.WriteLine("--------------------------------------------------");
                
                // 5. Get file info
                Console.WriteLine("\n5. Getting file information...");
                var fileInfo = await client.GetFileInfoAsync("examples/test.txt");
                Console.WriteLine($"Name: {fileInfo.Name}");
                Console.WriteLine($"Path: {fileInfo.Path}");
                Console.WriteLine($"Size: {fileInfo.Size} bytes");
                Console.WriteLine($"Created: {fileInfo.Created}");
                Console.WriteLine($"Modified: {fileInfo.Modified}");
                Console.WriteLine($"Attributes: {fileInfo.Attributes}");
                
                // 6. Create subdirectory
                Console.WriteLine("\n6. Creating a subdirectory...");
                await client.CreateDirectoryAsync("examples/subdir");
                
                // 7. Write binary file (using base64 encoding)
                Console.WriteLine("\n7. Writing binary file...");
                byte[] binaryData = new byte[32];
                new Random().NextBytes(binaryData); // Generate random binary data
                string base64Content = Convert.ToBase64String(binaryData);
                await client.WriteFileAsync("examples/subdir/binary.dat", base64Content, "base64");
                
                // 8. Check path exists
                Console.WriteLine("\n8. Checking if paths exist...");
                var pathInfo1 = await client.CheckExistsAsync("examples/test.txt");
                Console.WriteLine($"Path 'examples/test.txt' exists: {pathInfo1.Exists} (Type: {pathInfo1.Type})");
                
                var pathInfo2 = await client.CheckExistsAsync("examples/nonexistent.txt");
                Console.WriteLine($"Path 'examples/nonexistent.txt' exists: {pathInfo2.Exists} (Type: {pathInfo2.Type})");
                
                // 9. Delete file
                Console.WriteLine("\n9. Deleting file...");
                await client.DeleteFileAsync("examples/test.txt");
                
                // 10. Delete directory (with recursive flag to remove contents)
                Console.WriteLine("\n10. Deleting directory recursively...");
                await client.DeleteDirectoryAsync("examples", true);
                
                // 11. List accessible directories
                Console.WriteLine("\n11. Listing accessible directories...");
                var accessibleDirs = await client.ListAccessibleDirectoriesAsync();
                foreach (var dir in accessibleDirs)
                {
                    Console.WriteLine($"Directory: {dir.Name} (Alias: {dir.Alias}, Path: {dir.Path})");
                }
                
                // 12. Create a file in the second directory
                Console.WriteLine("\n12. Creating a file in the second directory...");
                await client.WriteFileAsync("dir2:/test.txt", "This is a test file in the second directory.");
                
                // 13. Get directory tree
                Console.WriteLine("\n13. Getting directory tree...");
                var tree = await client.GetDirectoryTreeAsync("dir2:/");
                Console.WriteLine("Directory tree:");
                Helpers.PrintDirectoryTree(new MCPFileSystem.Example.DirectoryTreeNode {
                    Name = tree.Name,
                    Type = tree.Type,
                    Size = tree.Size,
                    Children = ConvertChildren(tree.Children)
                }, 0);
                
                // 14. Search for files
                Console.WriteLine("\n14. Searching for files...");
                // First create some more files for the search
                await client.WriteFileAsync("dir1:/searchtest1.txt", "Content 1");
                await client.WriteFileAsync("dir1:/searchtest2.log", "Content 2");
                await client.WriteFileAsync("dir2:/searchtest3.txt", "Content 3");
                
                var searchResults = await client.SearchFilesAsync(".", "*test*.txt");
                Console.WriteLine($"Found {searchResults.Count} files:");
                foreach (var result in searchResults)
                {
                    Console.WriteLine($"  {result.Path} ({result.Type})");
                }
                
                // 15. Move a file
                Console.WriteLine("\n15. Moving a file...");
                await client.MoveFileAsync("dir1:/searchtest1.txt", "dir2:/moved_file.txt");
                var pathInfo = await client.CheckExistsAsync("dir2:/moved_file.txt");
                Console.WriteLine($"Moved file exists: {pathInfo.Exists} (Type: {pathInfo.Type})");
                
                // 16. Edit a file
                Console.WriteLine("\n16. Editing a file...");
                // First create a file to edit
                await client.WriteFileAsync("dir1:/edit_test.txt", "This is line 1.\nThis is line 2.\nThis is line 3.");
                
                // Then edit it
                var editResult = await client.EditFileAsync("dir1:/edit_test.txt", 
                    new List<MCPFileSystem.Client.EditOperation> {
                        new MCPFileSystem.Client.EditOperation { OldText = "This is line 2.", NewText = "This is MODIFIED line 2." }
                    }
                );
                
                Console.WriteLine($"Edit result: {editResult.EditCount} edits applied");
                Console.WriteLine("Diff:\n" + editResult.Diff);
                
                // Read the edited file to verify changes
                string editedContent = await client.ReadFileAsync("dir1:/edit_test.txt");
                Console.WriteLine("Edited file content:");
                Console.WriteLine("--------------------------------------------------");
                Console.WriteLine(editedContent);
                Console.WriteLine("--------------------------------------------------");
                
                Console.WriteLine("\nMCP Filesystem operations completed successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Shutdown the server process
                Console.WriteLine("\nShutting down server...");
                try
                {
                    if (!serverProcess.HasExited)
                    {
                        serverProcess.Kill();
                        serverProcess.WaitForExit(5000);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error shutting down server: {ex.Message}");
                }
                
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
            }
        }
        
        private static List<MCPFileSystem.Example.DirectoryTreeNode> ConvertChildren(List<MCPFileSystem.Client.DirectoryTreeNode> children)
        {
            if (children == null) return null;
            
            var result = new List<MCPFileSystem.Example.DirectoryTreeNode>();
            
            foreach (var child in children)
            {
                result.Add(new MCPFileSystem.Example.DirectoryTreeNode
                {
                    Name = child.Name,
                    Type = child.Type,
                    Size = child.Size,
                    Children = ConvertChildren(child.Children)
                });
            }
            
            return result;
        }
    }
}
