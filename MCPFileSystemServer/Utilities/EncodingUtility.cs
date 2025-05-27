using MCPFileSystem.Contracts;
using System.Text;

namespace MCPFileSystemServer.Utilities;

/// <summary>
/// Utility class for handling file encoding operations.
/// </summary>
public static class EncodingUtility
{
    /// <summary>
    /// Converts a FileEncoding enum value to a System.Text.Encoding instance.
    /// </summary>
    /// <param name="fileEncoding">The FileEncoding to convert.</param>
    /// <returns>The corresponding System.Text.Encoding instance.</returns>
    public static Encoding ToSystemEncoding(FileEncoding fileEncoding)
    {
        return fileEncoding switch
        {
            FileEncoding.Utf8NoBom => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            FileEncoding.Utf8WithBom => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            FileEncoding.Ascii => Encoding.ASCII,
            FileEncoding.Utf16Le => Encoding.Unicode, // UTF-16 Little Endian
            FileEncoding.Utf16Be => Encoding.BigEndianUnicode, // UTF-16 Big Endian
            FileEncoding.Utf32Le => Encoding.UTF32,
            FileEncoding.SystemDefault => Encoding.Default,
            FileEncoding.AutoDetect => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), // Default to UTF-8 for writing
            _ => throw new ArgumentException($"Unsupported file encoding: {fileEncoding}", nameof(fileEncoding))
        };
    }

    /// <summary>
    /// Attempts to detect the encoding of a file by examining its byte order mark (BOM) and content.
    /// </summary>
    /// <param name="filePath">The path to the file to analyze.</param>
    /// <returns>The detected FileEncoding, or AutoDetect if unable to determine.</returns>
    public static async Task<FileEncoding> DetectFileEncodingAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return FileEncoding.Utf8NoBom; // Default for new files
        }        try
        {
            // Read the first few bytes to check for BOM
            var buffer = new byte[4];
            using (var stream = File.OpenRead(filePath))
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (bytesRead < buffer.Length)
                {
                    Array.Resize(ref buffer, bytesRead);
                }
            }

            // Check for UTF-8 BOM
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
            {
                return FileEncoding.Utf8WithBom;
            }

            // Check for UTF-16 Little Endian BOM
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
            {
                // Could be UTF-16 LE or UTF-32 LE, check for UTF-32
                if (buffer.Length >= 4 && buffer[2] == 0x00 && buffer[3] == 0x00)
                {
                    return FileEncoding.Utf32Le;
                }
                return FileEncoding.Utf16Le;
            }

            // Check for UTF-16 Big Endian BOM
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
            {
                return FileEncoding.Utf16Be;
            }

            // No BOM detected, try to determine encoding by content analysis
            return await AnalyzeFileContentAsync(filePath);
        }
        catch
        {
            // If detection fails, default to UTF-8 without BOM
            return FileEncoding.Utf8NoBom;
        }
    }

    /// <summary>
    /// Analyzes file content to determine encoding when no BOM is present.
    /// </summary>
    /// <param name="filePath">The path to the file to analyze.</param>
    /// <returns>The detected FileEncoding based on content analysis.</returns>
    private static async Task<FileEncoding> AnalyzeFileContentAsync(string filePath)
    {        try
        {
            // Read a sample of the file content
            var buffer = new byte[Math.Min(8192, (int)new FileInfo(filePath).Length)];
            using (var stream = File.OpenRead(filePath))
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length));
                if (bytesRead < buffer.Length)
                {
                    Array.Resize(ref buffer, bytesRead);
                }
            }

            // Check if content is valid ASCII
            if (IsValidAscii(buffer))
            {
                return FileEncoding.Ascii;
            }

            // Check if content is valid UTF-8
            if (IsValidUtf8(buffer))
            {
                return FileEncoding.Utf8NoBom;
            }

            // Default to UTF-8 if uncertain
            return FileEncoding.Utf8NoBom;
        }
        catch
        {
            return FileEncoding.Utf8NoBom;
        }
    }

    /// <summary>
    /// Checks if the byte array contains only valid ASCII characters.
    /// </summary>
    /// <param name="bytes">The byte array to check.</param>
    /// <returns>True if all bytes are valid ASCII, false otherwise.</returns>
    private static bool IsValidAscii(byte[] bytes)
    {
        foreach (byte b in bytes)
        {
            if (b > 127)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Checks if the byte array contains valid UTF-8 sequences.
    /// </summary>
    /// <param name="bytes">The byte array to check.</param>
    /// <returns>True if the content appears to be valid UTF-8, false otherwise.</returns>
    private static bool IsValidUtf8(byte[] bytes)
    {
        try
        {
            var decoder = Encoding.UTF8.GetDecoder();
            decoder.Fallback = DecoderFallback.ExceptionFallback;
            
            var charCount = decoder.GetCharCount(bytes, 0, bytes.Length, flush: true);
            var chars = new char[charCount];
            decoder.GetChars(bytes, 0, bytes.Length, chars, 0, flush: true);
            
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }

    /// <summary>
    /// Converts a System.Text.Encoding to the closest FileEncoding enum value.
    /// </summary>
    /// <param name="encoding">The System.Text.Encoding to convert.</param>
    /// <returns>The corresponding FileEncoding enum value.</returns>
    public static FileEncoding FromSystemEncoding(Encoding encoding)
    {
        return encoding.EncodingName switch
        {
            var name when name.Contains("UTF-8") => 
                encoding.GetPreamble().Length > 0 ? FileEncoding.Utf8WithBom : FileEncoding.Utf8NoBom,
            var name when name.Contains("Unicode (UTF-16)") && encoding == Encoding.Unicode => 
                FileEncoding.Utf16Le,
            var name when name.Contains("Unicode (UTF-16 Big-Endian)") => 
                FileEncoding.Utf16Be,
            var name when name.Contains("UTF-32") => 
                FileEncoding.Utf32Le,
            var name when name.Contains("US-ASCII") => 
                FileEncoding.Ascii,
            _ => FileEncoding.SystemDefault
        };
    }
}
