using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace MCPFileSystem.Contracts;

/// <summary>
/// Custom JSON converter for EditType enum that handles string values correctly
/// </summary>
public class EditTypeConverter : JsonConverter<EditType>
{
    public override EditType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var value = reader.GetString();
            if (string.Equals(value, "Replace", StringComparison.OrdinalIgnoreCase))
            {
                return EditType.Replace;
            }
            throw new JsonException($"Invalid EditType value: '{value}'. Only 'Replace' is supported.");
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            var value = reader.GetInt32();
            if (value == 2) // Replace = 2
            {
                return EditType.Replace;
            }
            throw new JsonException($"Invalid EditType numeric value: {value}. Only 2 (Replace) is supported.");
        }

        throw new JsonException($"Unexpected token type for EditType: {reader.TokenType}");
    }

    public override void Write(Utf8JsonWriter writer, EditType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// Helper class for serializing and deserializing FileEdit objects with proper validation
/// </summary>
public static class FileEditJsonHelper
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new EditTypeConverter() },
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
            foreach (var edit in edits)
            {
                edit.NormalizeText();
                var validation = edit.Validate();
                if (!validation.IsValid)
                {
                    errors.AddRange(validation.Errors.Select(e => $"Edit at line {edit.LineNumber}: {e}"));
                }
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
            else if (ex.Message.Contains("EditType"))
            {
                errors.Add("Hint: EditType must be \"Replace\" (case-sensitive string value).");
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
                LineNumber = 1,
                Type = EditType.Replace,
                Text = "// Single line replacement"
            },
            new()
            {
                LineNumber = 5,
                Type = EditType.Replace,
                Text = "function example() {\\n    console.log(\"Multi-line with escaped quotes\");\\n}",
                OldText = "old function content"
            }
        };

        return SerializeEdits(example);
    }
}
