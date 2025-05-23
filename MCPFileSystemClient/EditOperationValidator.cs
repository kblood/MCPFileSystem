using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace MCPFileSystem.Client
{
    /// <summary>
    /// Helper class for validating and formatting EditOperation JSON for file edits
    /// </summary>
    public static class EditOperationValidator
    {
        /// <summary>
        /// Validates edit operations to ensure they are properly formatted with correctly escaped newlines and special characters
        /// </summary>
        /// <param name="operations">List of operations to validate</param>
        /// <returns>True if all operations are valid, false otherwise</returns>
        public static bool ValidateOperations(List<EditOperation> operations)
        {
            if (operations == null || operations.Count == 0)
            {
                return false;
            }

            foreach (var op in operations)
            {
                // For INSERT and REPLACE, Text is required
                if ((op.Type == "INSERT" || op.Type == "REPLACE") && string.IsNullOrEmpty(op.Text))
                {
                    Console.WriteLine($"Error: {op.Type} operation at line {op.LineNumber} has no Text content");
                    return false;
                }

                // Validate line number
                if (op.LineNumber <= 0)
                {
                    Console.WriteLine($"Error: Invalid LineNumber {op.LineNumber} (must be 1-based)");
                    return false;
                }

                // Check if Text contains unescaped backslashes at line ends which might indicate improperly formatted newlines
                if (!string.IsNullOrEmpty(op.Text) && op.Text.Contains("\\") && !op.Text.Contains("\\n") && (op.Text.Contains("\\\r") || op.Text.Contains("\\\n")))
                {
                    Console.WriteLine("Warning: Text appears to contain backslashes at line ends. Newlines should be represented as '\\n'");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Formats multi-line text properly for JSON serialization ensuring newlines are represented as \n
        /// </summary>
        /// <param name="text">The multi-line text to format</param>
        /// <returns>Properly formatted text string for JSON serialization</returns>
        public static string FormatMultiLineTextForJson(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            // Replace all types of newlines with \n
            string normalized = text
                .Replace("\r\n", "\n")
                .Replace("\r", "\n");
            
            return normalized;
        }

        /// <summary>
        /// Checks if a given JSON string for edit operations is properly formatted with correct newline escaping
        /// </summary>
        /// <param name="jsonString">The JSON string to validate</param>
        /// <returns>True if the JSON is valid, false otherwise with console error output</returns>
        public static bool ValidateEditOperationsJson(string jsonString)
        {
            try
            {
                // Attempt to deserialize
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var operations = JsonSerializer.Deserialize<List<EditOperation>>(jsonString, options);
                
                // Check if operations were deserialized and validate them
                if (operations != null)
                {
                    return ValidateOperations(operations);
                }
                return false;
            }
            catch (JsonException je)
            {
                Console.WriteLine($"JSON Error: {je.Message}");
                Console.WriteLine($"Invalid JSON: {jsonString}");
                return false;
            }
        }

        /// <summary>
        /// Creates a properly formatted JSON string for edit operations
        /// </summary>
        /// <param name="operations">List of operations</param>
        /// <returns>JSON string ready for use in EditFileAsync</returns>
        public static string CreateEditOperationsJson(List<EditOperation> operations)
        {
            // First ensure all texts are properly formatted
            foreach (var op in operations)
            {
                if (!string.IsNullOrEmpty(op.Text))
                {
                    op.Text = FormatMultiLineTextForJson(op.Text);
                }
            }

            // Validate before serializing
            if (!ValidateOperations(operations))
            {
                throw new ArgumentException("Invalid edit operations provided");
            }

            return JsonSerializer.Serialize(operations);
        }
    }
}
