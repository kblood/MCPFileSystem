using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Text.Json;

namespace MCPFileSystemServer.Tools;

/// <summary>
/// Provides thinking tools for the MCP server.
/// </summary>
[McpServerToolType]
public static class ThinkTools
{
    /// <summary>
    /// Provides a space for structured thinking during complex operations, without making any state changes.
    /// </summary>
    /// <param name="thought">The thought or reasoning to process.</param>
    /// <returns>A JSON string confirming the thought was received.</returns>
    [McpServerTool("think")]
    [Description("Provides a space for structured thinking during complex operations, without making any state changes.")]
    public static string Think(
        [Description("The thought or reasoning to process")]
        string thought)
    {
        var response = new
        {
            Received = true,
            Thought = thought.Substring(0, Math.Min(50, thought.Length)) + (thought.Length > 50 ? "..." : "")
        };
        
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true });
    }
}
