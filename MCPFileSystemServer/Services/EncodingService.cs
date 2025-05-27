using MCPFileSystem.Contracts;
using System.Text;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Service for handling file encoding operations.
/// </summary>
public static class EncodingService
{
    /// <summary>
    /// Converts a FileEncoding enum to a System.Text.Encoding instance.
    /// </summary>
    /// <param name="fileEncoding">The file encoding to convert.</param>
    /// <returns>The corresponding System.Text.Encoding instance.</returns>
    public static Encoding GetSystemEncoding(FileEncoding fileEncoding)
    {
        return fileEncoding switch
        {
            FileEncoding.Utf8NoBom => new UTF8Encoding(false), // UTF-8 without BOM
            FileEncoding.Utf8WithBom => new UTF8Encoding(true), // UTF-8 with BOM
            FileEncoding.Ascii => Encoding.ASCII,
            FileEncoding.Utf16Le => Encoding.Unicode, // UTF-16 LE with BOM
            FileEncoding.Utf16Be => Encoding.BigEndianUnicode, // UTF-16 BE with BOM
            FileEncoding.Utf32Le => Encoding.UTF32, // UTF-32 LE with BOM
            FileEncoding.SystemDefault => Encoding.Default,
            FileEncoding.AutoDetect => new UTF8Encoding(false), // Default to UTF-8 without BOM for writing
            _ => new UTF8Encoding(false) // Safe default
        };
    }

    /// <summary>
    /// Detects the encoding of a file by reading its byte order mark (BOM) and content.
    /// </summary>
    /// <param name="filePath">Path to the file to analyze.</param>
    /// <returns>The detected FileEncoding, or Utf8NoBom if detection fails.</returns>
    public static async Task<FileEncoding> DetectFileEncodingAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return FileEncoding.Utf8NoBom;
        }

        try
        {
            // Read the first few bytes to check for BOM
            byte[] buffer = new byte[4];
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                int bytesRead = await fileStream.ReadAsync(buffer, 0, 4);
                
                if (bytesRead >= 3)
                {
                    // UTF-8 BOM: EF BB BF
                    if (buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                        return FileEncoding.Utf8WithBom;
                    
                    // UTF-16 LE BOM: FF FE
                    if (buffer[0] == 0xFF && buffer[1] == 0xFE)
                    {
                        // Check if it's UTF-32 LE: FF FE 00 00
                        if (bytesRead >= 4 && buffer[2] == 0x00 && buffer[3] == 0x00)
                            return FileEncoding.Utf32Le;
                        return FileEncoding.Utf16Le;
                    }
                    
                    // UTF-16 BE BOM: FE FF
                    if (buffer[0] == 0xFE && buffer[1] == 0xFF)
                        return FileEncoding.Utf16Be;
                }

                // If no BOM detected, assume UTF-8 without BOM
                // (More sophisticated detection could be added here if needed)
                return FileEncoding.Utf8NoBom;
            }
        }
        catch
        {
            // If detection fails, default to UTF-8 without BOM
            return FileEncoding.Utf8NoBom;
        }
    }

    /// <summary>
    /// Gets the default FileWriteOptions with UTF-8 without BOM.
    /// </summary>
    /// <returns>Default FileWriteOptions instance.</returns>
    public static FileWriteOptions GetDefaultWriteOptions()
    {
        return new FileWriteOptions
        {
            Encoding = FileEncoding.Utf8NoBom,
            PreserveOriginalEncoding = false
        };
    }

    /// <summary>
    /// Determines the encoding to use for a file operation based on options and existing file.
    /// </summary>
    /// <param name="filePath">Path to the file.</param>
    /// <param name="options">Write options, or null for defaults.</param>
    /// <returns>The FileEncoding to use and the corresponding System.Text.Encoding.</returns>
    public static async Task<(FileEncoding fileEncoding, Encoding systemEncoding)> DetermineEncodingAsync(string filePath, FileWriteOptions? options = null)
    {
        options ??= GetDefaultWriteOptions();

        FileEncoding targetEncoding;

        if (options.PreserveOriginalEncoding && File.Exists(filePath))
        {
            // Try to preserve the original file's encoding
            targetEncoding = await DetectFileEncodingAsync(filePath);
        }
        else
        {
            // Use the specified encoding
            targetEncoding = options.Encoding;
        }

        var systemEncoding = GetSystemEncoding(targetEncoding);
        return (targetEncoding, systemEncoding);
    }

    /// <summary>
    /// Reads a file with automatic encoding detection.
    /// </summary>
    /// <param name="filePath">Path to the file to read.</param>
    /// <returns>The file content and the detected encoding.</returns>
    public static async Task<(string content, FileEncoding detectedEncoding)> ReadFileWithEncodingDetectionAsync(string filePath)
    {
        var detectedEncoding = await DetectFileEncodingAsync(filePath);
        var systemEncoding = GetSystemEncoding(detectedEncoding);
        
        string content = await File.ReadAllTextAsync(filePath, systemEncoding);
        return (content, detectedEncoding);
    }
}
