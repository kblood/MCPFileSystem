namespace MCPFileSystem.Contracts;

/// <summary>
/// Options for file writing operations.
/// </summary>
public class FileWriteOptions
{
    /// <summary>
    /// The encoding to use when writing files.
    /// Default is UTF8 without BOM.
    /// </summary>
    public FileEncoding Encoding { get; set; } = FileEncoding.Utf8NoBom;

    /// <summary>
    /// Whether to preserve existing encoding when editing files.
    /// If true, the original file's encoding will be detected and preserved.
    /// If false, the specified Encoding will be used.
    /// Default is false.
    /// </summary>
    public bool PreserveOriginalEncoding { get; set; } = false;
}

/// <summary>
/// Supported file encodings.
/// </summary>
public enum FileEncoding
{
    /// <summary>
    /// UTF-8 without Byte Order Mark (BOM).
    /// This is the most common encoding for text files.
    /// </summary>
    Utf8NoBom,

    /// <summary>
    /// UTF-8 with Byte Order Mark (BOM).
    /// Used by some Windows applications.
    /// </summary>
    Utf8WithBom,

    /// <summary>
    /// ASCII encoding.
    /// Only supports characters 0-127.
    /// </summary>
    Ascii,

    /// <summary>
    /// UTF-16 Little Endian with BOM.
    /// Used by Windows for some system files.
    /// </summary>
    Utf16Le,

    /// <summary>
    /// UTF-16 Big Endian with BOM.
    /// Less commonly used.
    /// </summary>
    Utf16Be,

    /// <summary>
    /// UTF-32 Little Endian with BOM.
    /// Rarely used, but supported.
    /// </summary>
    Utf32Le,

    /// <summary>
    /// System default encoding (typically based on locale).
    /// Use with caution as it varies by system.
    /// </summary>
    SystemDefault,

    /// <summary>
    /// Auto-detect encoding when reading, use UTF8 without BOM when writing.
    /// This is a special mode for maximum compatibility.
    /// </summary>
    AutoDetect
}
