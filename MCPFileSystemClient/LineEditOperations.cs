using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

namespace MCPFileSystem.Client
{
    /// <summary>
    /// Extension to the MCPFileSystemClient class for simple text replacement operations compatible with the edit_file tool
    /// </summary>
    public partial class MCPFileSystemClient
    {
        /// <summary>
        /// Performs simple text replacements on a file using the format compatible with edit_file
        /// </summary>
        /// <param name="path">Path to the file to edit</param>
        /// <param name="operations">List of text replacement operations</param>
        /// <param name="dryRun">Whether to perform a dry run that simulates the changes without applying them</param>
        /// <returns>Result of the edit operation</returns>
        public async Task<EditResult> EditFileAsync(string path, List<FileEdit> operations, bool dryRun = false)
        {
            if (operations == null || operations.Count == 0)
                throw new ArgumentException("No edit operations provided");

            // Validate and normalize operations
            foreach (var op in operations)
            {
                op.NormalizeText();
                var validation = op.Validate();
                if (!validation.IsValid)
                    throw new ArgumentException($"Invalid edit: {validation.GetErrorMessage()}");
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
            var response = await SendMCPRequestAsync("edit", parameters);
            var parsedResponse = ParseResponse(response);

            if (!parsedResponse.IsSuccess)
                throw new Exception($"Failed to edit file: {parsedResponse.Data}");

            if (string.IsNullOrEmpty(parsedResponse.Data))
                return new EditResult { EditCount = 0, Diff = dryRun ? "Dry run resulted in no changes" : "No changes applied" };

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
    }
}
