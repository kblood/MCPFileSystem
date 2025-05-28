namespace MCPFileSystem.Contracts;

/// <summary>
/// Represents a Replace edit operation to be applied to a file.
/// Only Replace operations are supported for reliable text editing.
/// </summary>
public class FileEdit
{
    /// <summary>
    /// The 1-based line number where the replace operation should occur.
    /// For appending to the end of the file, use a number greater than the last line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// The type of edit operation. Must be Replace.
    /// </summary>
    public EditType Type { get; set; } = EditType.Replace;

    /// <summary>
    /// The text to replace with.
    /// For multi-line content, use \n for newlines in JSON.
    /// For literal quotes, use \" in JSON.
    /// For literal backslashes, use \\ in JSON.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// The text to find on the specified LineNumber.
    /// If null, the entire line specified by LineNumber is replaced with Text.
    /// If provided, only this specific string within the line is replaced.
    /// </summary>
    public string? OldText { get; set; }

    /// <summary>
    /// Validates this FileEdit instance for common issues.
    /// </summary>
    /// <returns>A validation result with any errors found.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (LineNumber <= 0)
        {
            errors.Add($"LineNumber must be 1 or greater (got {LineNumber})");
        }

        if (Type != EditType.Replace)
        {
            errors.Add($"Only Replace operations are supported (got {Type})");
        }

        if (string.IsNullOrEmpty(Text))
        {
            errors.Add("Text is required for Replace operations");
        }

        // Check for common JSON formatting issues in Text
        if (!string.IsNullOrEmpty(Text))
        {
            if (Text.Contains("\r\n") || Text.Contains("\r"))
            {
                errors.Add("Text contains literal line breaks. Use \\n in JSON instead");
            }

            if (Text.Contains("\\") && !Text.Contains("\\n") && !Text.Contains("\\\"") && !Text.Contains("\\\\"))
            {
                errors.Add("Text may contain improperly escaped backslashes");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Normalizes the Text property by converting different newline formats to \n
    /// </summary>
    public void NormalizeText()
    {
        if (!string.IsNullOrEmpty(Text))
        {
            Text = Text.Replace("\r\n", "\n").Replace("\r", "\n");
        }
        
        if (!string.IsNullOrEmpty(OldText))
        {
            OldText = OldText.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}

/// <summary>
/// Defines the type of edit operation.
/// Only Replace operations are supported for reliable text editing.
/// </summary>
public enum EditType
{
    /// <summary>
    /// Replace an entire line or specific text within a line.
    /// If OldText is null, the entire LineNumber is replaced with Text.
    /// If OldText is provided, only that part of the line is replaced with Text.
    /// </summary>
    Replace = 2  // Keep the same numeric value for backward compatibility
}

/// <summary>
/// Represents the result of a validation operation.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; }
    public List<string> Errors { get; }

    public ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors ?? new List<string>();
    }

    public string GetErrorMessage()
    {
        return IsValid ? string.Empty : string.Join("; ", Errors);
    }
}