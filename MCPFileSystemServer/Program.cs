using Microsoft.Extensions.Hosting;
using ModelContextProtocol;
using System.Reflection;
using System.IO;

namespace MCPFileSystemServer;

/// <summary>
/// Entry point for the MCPFileSystem MCP server, which implements a Model Context Protocol (MCP) server
/// for providing file system operations to Large Language Models.
/// 
/// This server is built using the official C# SDK for MCP (https://github.com/modelcontextprotocol/csharp-sdk)
/// and provides tools for:
/// - File system operations (reading, writing, editing files)
/// - Directory operations (listing, creating, tree views)
/// - File searching and metadata retrieval
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task Main(string[] args)
    {
        // Process command line arguments if needed
        ProcessCommandLineArgs(args);

        try
        {
            // Suppress console output to avoid interfering with the MCP protocol
            Console.SetOut(TextWriter.Null);
            
            // Set up error logging to a file
            var logFile = Path.Combine(Path.GetTempPath(), "mcpfilesystem-log.txt");
            using var errorWriter = new StreamWriter(logFile, true);
            Console.SetError(errorWriter);
            
            Console.Error.WriteLine($"Starting MCP server at {DateTime.Now}");
            Console.Error.WriteLine($"Base directory: {Services.FileValidationService.BaseDirectory}");
            Console.Error.WriteLine($"Log file: {logFile}");
            
            // Build and run the MCP server
            var builder = Host.CreateEmptyApplicationBuilder(null);
            builder.Services
                .AddMcpServer(options =>
                {
                    options.ServerInfo = new() { Name = "MCPFileSystemServer", Version = "1.0" };
                })
                .WithStdioServerTransport()
                .WithToolsFromAssembly(Assembly.GetExecutingAssembly());

            await builder.Build().RunAsync();
        }
        catch (Exception ex)
        {
            // Log errors to stderr so they don't interfere with the protocol
            Console.Error.WriteLine($"MCP server error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return; // Exit process on error
        }
    }

    /// <summary>
    /// Processes command line arguments.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static void ProcessCommandLineArgs(string[] args)
    {
        // Process command line arguments
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--root" && i + 1 < args.Length)
            {
                var rootDir = args[i + 1];
                if (Directory.Exists(rootDir))
                {
                    Services.FileValidationService.SetBaseDirectory(rootDir);
                    Console.Error.WriteLine($"Base directory set to: {rootDir}");
                }
                else
                {
                    Console.Error.WriteLine($"Warning: Directory not found: {rootDir}");
                }
                i++;
            }
            else if (args[i] == "--dir" && i + 1 < args.Length)
            {
                var additionalDir = args[i + 1];
                try
                {
                    if (Services.FileValidationService.AddAccessibleDirectory(additionalDir))
                    {
                        Console.Error.WriteLine($"Added accessible directory: {additionalDir}");
                    }
                    else
                    {
                        Console.Error.WriteLine($"Directory already accessible: {additionalDir}");
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error adding directory: {ex.Message}");
                }
                i++;
            }
        }
    }
}
