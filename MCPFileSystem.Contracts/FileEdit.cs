namespace MCPFileSystem.Contracts;

/// <summary>
/// Represents an edit operation to be applied to a file.
/// </summary>
public class FileEdit
{
    /// <summary>
    /// The 1-based line number where the edit should occur.
    /// For inserting at the beginning of the file, use 1.
    /// For appending to the end of the file, use a number greater than the last line number, or int.MaxValue.
    /// For replacing or deleting a line, this is the line to target.
    /// For ReplaceSection operations, this is the start line of the section to replace.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// The type of edit operation.
    /// </summary>
    public EditType Type { get; set; }

    /// <summary>
    /// The text to insert or replace with.
    /// For delete operations, this can be null or empty.
    /// </summary>
    public string? Text { get; set; }

    /// <summary>
    /// For replace operations, this is the text to find on the specified LineNumber.
    /// If null, the entire line specified by LineNumber is replaced with Text.
    /// If provided, only this specific string within the line is replaced.
    /// For ReplaceSection operations, this is ignored.
    /// Not used for Insert or Delete operations.
    /// </summary>
    public string? OldText { get; set; }

    /// <summary>
    /// For ReplaceSection operations, this is the ending line number (inclusive) of the section to replace.
    /// Must be greater than or equal to LineNumber.
    /// Not used for other operation types.
    /// </summary>
    public int? EndLine { get; set; }
}

/// <summary>
/// Defines the type of edit operation.
/// </summary>
public enum EditType
{
    /// <summary>
    /// Insert new text at the specified line number.
    /// If LineNumber is 1, inserts before the first line.
    /// If LineNumber is greater than total lines, appends to the end.
    /// Otherwise, inserts before the specified LineNumber.
    /// </summary>
    Insert,

    /// <summary>
    /// Delete the specified line number.
    /// </summary>
    Delete,

    /// <summary>
    /// Replace an entire line or specific text within a line.
    /// If OldText is null, the entire LineNumber is replaced with Text.
    /// If OldText is provided, only that part of the line is replaced with Text.
    /// </summary>
    Replace,

    /// <summary>
    /// Replace a section of the file from LineNumber to EndLine (inclusive) with the new text.
    /// The EndLine property must be set and must be >= LineNumber.
    /// This allows for more efficient replacement of larger file sections.
    /// </summary>
    ReplaceSection
}