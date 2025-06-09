using System.Text.Json;
using System.Collections.Generic;
using System.Linq;

namespace MCPFileSystem.Contracts;

/// <summary>
/// Helper class for serializing and deserializing FileEdit objects with proper validation
/// </summary>
public static class FileEditJsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    /// <summary>
    /// Deserializes a JSON string to a list of FileEdit objects with validation
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A tuple containing the deserialized edits and any validation errors</returns>
    public static (List<FileEdit>? Edits, List<string> Errors) DeserializeEdits(string json)
    {
        var errors = new List<string>();
        
        if (string.IsNullOrWhiteSpace(json))
        {
            errors.Add("JSON string is null or empty");
            return (null, errors);
        }

        try
        {
            var edits = JsonSerializer.Deserialize<List<FileEdit>>(json, DefaultOptions);
            if (edits == null)
            {
                errors.Add("Deserialized edits list is null");
                return (null, errors);
            }

            // Empty array is valid for some operations, but we'll return it with no errors
            if (edits.Count == 0)
            {
                return (edits, errors); // Return empty list with no errors
            }

            // Validate each edit and normalize text
            int idx = 0;
            foreach (var edit in edits)
            {
                edit.NormalizeText();
                var validation = edit.Validate();
                if (!validation.IsValid)
                {
                    errors.AddRange(validation.Errors.Select(e => $"Edit #{idx + 1}: {e}"));
                }
                idx++;
            }

            return (edits, errors);
        }
        catch (JsonException ex)
        {
            errors.Add($"JSON parsing error: {ex.Message}");
            // Provide helpful hints for common JSON errors
            if (ex.Message.Contains("Unterminated string"))
            {
                errors.Add("Hint: Check for unescaped newlines. Use \\n instead of literal line breaks in JSON strings.");
            }
            else if (ex.Message.Contains("Invalid character"))
            {
                errors.Add("Hint: Check for unescaped quotes. Use \\\" for literal quotes in JSON strings.");
            }
            return (null, errors);
        }
    }

    /// <summary>
    /// Serializes a list of FileEdit objects to JSON with proper formatting
    /// </summary>
    /// <param name="edits">The edits to serialize</param>
    /// <returns>The JSON string representation</returns>
    public static string SerializeEdits(List<FileEdit> edits)
    {
        return JsonSerializer.Serialize(edits, DefaultOptions);
    }

    /// <summary>
    /// Creates a formatted example of proper JSON for documentation
    /// </summary>
    /// <returns>Example JSON string showing proper format</returns>
    public static string GetExampleJson()
    {
        var example = new List<FileEdit>
        {
            new()
            {
                OldText = "console.log('old message');",
                Text = "console.log('new message');"
            },
            new()
            {
                OldText = "function test() {\\n    // old code\\n}",
                Text = "function test() {\\n    // new code\\n}"
            }
        };
        return SerializeEdits(example);
    }
}
