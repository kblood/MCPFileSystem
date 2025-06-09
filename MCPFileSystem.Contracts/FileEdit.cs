namespace MCPFileSystem.Contracts;

/// <summary>
/// Represents a simple text replacement operation to be applied to a file.
/// Only global text replacements are supported for reliable editing.
/// </summary>
public class FileEdit
{
    /// <summary>
    /// The text to find in the file. The first occurrence will be replaced.
    /// </summary>
    public string? OldText { get; set; }

    /// <summary>
    /// The text to replace with.
    /// For multi-line content, use \n for newlines in JSON.
    /// For literal quotes, use \" in JSON.
    /// For literal backslashes, use \\ in JSON.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// Validates this FileEdit instance for common issues.
    /// </summary>
    /// <returns>A validation result with any errors found.</returns>
    public ValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrEmpty(OldText))
        {
            errors.Add("OldText is required for text replacement");
        }

        if (string.IsNullOrEmpty(Text))
        {
            errors.Add("Text is required for text replacement");
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
    /// Normalizes the Text and OldText properties by converting different newline formats to \n
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