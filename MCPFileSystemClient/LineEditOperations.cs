using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace MCPFileSystem.Client
{
    /// <summary>
    /// Extension to the MCPFileSystemClient class for proper line-based edit operations compatible with d94_edit_file format
    /// </summary>
    public partial class MCPFileSystemClient
    {
        /// <summary>
        /// Line-based edit operation model that matches the d94_edit_file tool format
        /// </summary>
        public class LineEditOperation
        {
            /// <summary>
            /// Line number where the edit should be applied (1-based)
            /// </summary>
            public int LineNumber { get; set; }
            
            /// <summary>
            /// Type of edit operation: INSERT, DELETE, or REPLACE
            /// </summary>
            public string Type { get; set; } = string.Empty;
            
            /// <summary>
            /// The text to insert or replace with (required for INSERT and REPLACE operations)
            /// Newlines must be represented as \n within this string when serialized to JSON
            /// </summary>
            public string Text { get; set; } = string.Empty;
        }

        /// <summary>
        /// Performs line-based edits on a file using the format compatible with d94_edit_file
        /// </summary>
        /// <param name="path">Path to the file to edit</param>
        /// <param name="operations">List of line-based edit operations</param>
        /// <param name="dryRun">Whether to perform a dry run that simulates the changes without applying them</param>
        /// <returns>Result of the edit operation</returns>
        public async Task<EditResult> EditFileWithLineOperationsAsync(string path, List<LineEditOperation> operations, bool dryRun = false)
        {
            try
            {
                Console.WriteLine($"EditFileWithLineOperationsAsync: {path}, {operations.Count} operations, DryRun: {dryRun}");
                
                // Validate operations
                foreach (var op in operations)
                {
                    // Basic validation
                    if (op.LineNumber <= 0)
                    {
                        throw new ArgumentException($"Invalid LineNumber: {op.LineNumber}, must be 1-based");
                    }
                    
                    if (!new[] { "INSERT", "DELETE", "REPLACE" }.Contains(op.Type))
                    {
                        throw new ArgumentException($"Invalid Type: {op.Type}, must be INSERT, DELETE, or REPLACE");
                    }
                    
                    if ((op.Type == "INSERT" || op.Type == "REPLACE") && string.IsNullOrEmpty(op.Text))
                    {
                        throw new ArgumentException($"{op.Type} operation at line {op.LineNumber} requires Text content");
                    }
                }
                
                // Create the request parameters
                var editsJson = JsonSerializer.Serialize(operations);
                var parameters = JsonSerializer.Serialize(new 
                { 
                    path, 
                    editsJson,
                    dryRun 
                });
                
                // Send the request
                var response = await SendMCPRequestAsync("lineedit", parameters);
                var parsedResponse = ParseResponse(response);
                
                if (!parsedResponse.IsSuccess)
                {
                    throw new Exception($"Failed to edit file: {parsedResponse.Data}");
                }
                
                if (string.IsNullOrEmpty(parsedResponse.Data))
                {
                    Console.WriteLine("WARNING: Empty data in response for EditFileWithLineOperationsAsync");
                    return new EditResult { EditCount = 0, Diff = dryRun ? "Dry run resulted in no changes" : "No changes applied" };
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
                Console.WriteLine($"ERROR in EditFileWithLineOperationsAsync: {ex.GetType().Name}: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Helper method to properly format multi-line text for use in LineEditOperation.Text to ensure
        /// newlines are correctly represented as \n in the JSON
        /// </summary>
        /// <param name="text">The multi-line text to format</param>
        /// <returns>Text with newlines standardized to \n</returns>
        public static string FormatTextForLineEdit(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }
            
            // Replace all common newline variants with \n
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
