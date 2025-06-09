using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using MCPFileSystem.Contracts;

namespace MCPFileSystem.Client
{
    // Extension methods for the MCPFileSystemClient class to add new features
    public partial class MCPFileSystemClient
    {
        public async Task<EditResult> EditFileAsync(string path, List<FileEdit> edits, bool dryRun = false)
        {
            if (edits == null || edits.Count == 0)
                throw new ArgumentException("No edit operations provided");

            // Validate and normalize edits
            foreach (var edit in edits)
            {
                edit.NormalizeText();
                var validation = edit.Validate();
                if (!validation.IsValid)
                    throw new ArgumentException($"Invalid edit: {validation.GetErrorMessage()}");
            }

            var editsJson = JsonSerializer.Serialize(edits);
            var parameters = JsonSerializer.Serialize(new { path, editsJson, dryRun });
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
