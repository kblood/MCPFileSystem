# MCP FileSystem Server - Encoding Control Solution

## Problem Analysis

The MCP FileSystem Server was creating files with UTF-8 BOM (Byte Order Mark) by default, which is often undesirable for text files. The issues were found in:

1. **WriteFileAsync** - Used `Encoding.UTF8` (includes BOM)
2. **EditFileAsync** - Used `File.WriteAllLinesAsync` without encoding (defaults to UTF-8 with BOM) 
3. **CreateFileAsync** - Used `Encoding.UTF8` (includes BOM)
4. **ReadFileAsync** - Used `Encoding.UTF8` but inconsistent with writing operations

## Solution Overview

Added comprehensive encoding control to the MCP FileSystem Server with:

### 1. New Contracts (`MCPFileSystem.Contracts\FileWriteOptions.cs`)

```csharp
public class FileWriteOptions
{
    public FileEncoding Encoding { get; set; } = FileEncoding.Utf8NoBom;
    public bool PreserveOriginalEncoding { get; set; } = false;
}

public enum FileEncoding
{
    Utf8NoBom,      // Default - UTF-8 without BOM
    Utf8WithBom,    // UTF-8 with BOM
    Ascii,          // ASCII encoding
    Utf16Le,        // UTF-16 Little Endian with BOM
    Utf16Be,        // UTF-16 Big Endian with BOM
    Utf32Le,        // UTF-32 Little Endian with BOM
    SystemDefault,  // System default encoding
    AutoDetect      // Auto-detect on read, UTF-8 no BOM on write
}
```

### 2. New Encoding Service (`Services\EncodingService.cs`)

Key features:
- **GetSystemEncoding()** - Converts FileEncoding to System.Text.Encoding
- **DetectFileEncodingAsync()** - Auto-detects file encoding by analyzing BOM
- **DetermineEncodingAsync()** - Determines encoding for operations based on options
- **ReadFileWithEncodingDetectionAsync()** - Reads files with automatic encoding detection

The service correctly handles BOM detection:
```csharp
public static Encoding GetSystemEncoding(FileEncoding fileEncoding)
{
    return fileEncoding switch
    {
        FileEncoding.Utf8NoBom => new UTF8Encoding(false),    // No BOM
        FileEncoding.Utf8WithBom => new UTF8Encoding(true),   // With BOM
        // ... other encodings
    };
}
```

### 3. Updated FileService Methods

All file writing methods now support encoding options:

- **WriteFileAsync(path, content, options)** - Added encoding support
- **EditFileAsync(path, edits, dryRun, options)** - Added encoding support  
- **CreateFileAsync(path, content, options)** - Added encoding support
- **ReadFileAsync()** - Now auto-detects encoding for consistency

### 4. Enhanced MCP Tools (`Tools\FileTools.cs`)

Updated tools with encoding parameters:

#### write_file
```csharp
public static async Task<string> WriteFile(
    string path,
    string content,
    string encoding = "utf8",                    // NEW: Encoding parameter
    bool preserveOriginalEncoding = false)       // NEW: Preserve option
```

#### edit_file
```csharp
public static async Task<string> EditFile(
    string path,
    string editsJson,
    bool dryRun = false,
    string encoding = "utf8",                    // NEW: Encoding parameter
    bool preserveOriginalEncoding = true)        // NEW: Preserve option (default true for edits)
```

#### detect_file_encoding (NEW TOOL)
```csharp
public static async Task<string> DetectFileEncoding(string path)
```

### 5. Encoding Parameter Options

The tools accept these encoding string values:
- `"utf8"` - UTF-8 without BOM (default)
- `"utf8-bom"` - UTF-8 with BOM
- `"ascii"` - ASCII encoding
- `"utf16le"` - UTF-16 Little Endian
- `"utf16be"` - UTF-16 Big Endian  
- `"utf32le"` - UTF-32 Little Endian
- `"system"` - System default encoding
- `"auto"` - Auto-detect encoding

## Key Benefits

1. **Default to UTF-8 without BOM** - Most compatible format
2. **Preserve original encoding** - Option to maintain existing file encoding when editing
3. **Auto-detection** - Automatically detect file encoding when reading
4. **Full control** - Explicit encoding control for all file operations
5. **Backward compatibility** - Default behavior uses UTF-8 without BOM
6. **New detection tool** - Utility to analyze file encodings

## Usage Examples

### Write file without BOM (default)
```json
{
  "tool": "write_file",
  "path": "test.txt", 
  "content": "Hello World",
  "encoding": "utf8"
}
```

### Write file with BOM
```json
{
  "tool": "write_file",
  "path": "test.txt",
  "content": "Hello World", 
  "encoding": "utf8-bom"
}
```

### Edit file preserving original encoding
```json
{
  "tool": "edit_file",
  "path": "existing.txt",
  "editsJson": "[{\"LineNumber\":1,\"Type\":0,\"Text\":\"New line\"}]",
  "preserveOriginalEncoding": true
}
```

### Detect file encoding
```json
{
  "tool": "detect_file_encoding",
  "path": "mystery.txt"
}
```

## Files Modified

1. `MCPFileSystem.Contracts\FileWriteOptions.cs` - **NEW** encoding options
2. `MCPFileSystem.Contracts\FileEdit.cs` - Added WriteOptions property
3. `Services\EncodingService.cs` - **NEW** encoding utilities
4. `Services\FileService.cs` - Updated all file write methods
5. `Tools\FileTools.cs` - Added encoding parameters and new detection tool

This solution provides complete control over file encoding while maintaining backward compatibility and defaulting to the most widely compatible UTF-8 without BOM format.
