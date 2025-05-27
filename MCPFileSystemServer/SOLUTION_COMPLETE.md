# Fixed MCP FileSystem Server - Encoding Control Implementation

## Problem Resolution Summary

✅ **FIXED**: The MCP FileSystem Server was creating files with UTF-8 BOM by default, which is often undesirable.

## Root Cause Analysis

The server was using .NET's `Encoding.UTF8` property, which **includes BOM by default**. This affected:

1. `WriteFileAsync()` - Used `Encoding.UTF8` (includes BOM)
2. `EditFileAsync()` - Used `File.WriteAllLinesAsync()` without encoding parameter (defaults to UTF-8 with BOM)  
3. `CreateFileAsync()` - Used `Encoding.UTF8` (includes BOM)

## Complete Solution Implemented

### 1. New Encoding Contracts (✅ COMPLETED)

**File: `MCPFileSystem.Contracts\FileWriteOptions.cs`**
```csharp
public class FileWriteOptions
{
    public FileEncoding Encoding { get; set; } = FileEncoding.Utf8NoBom; // Default: No BOM!
    public bool PreserveOriginalEncoding { get; set; } = false;
}

public enum FileEncoding
{
    Utf8NoBom,      // ✅ NEW DEFAULT - UTF-8 without BOM  
    Utf8WithBom,    // UTF-8 with BOM (old behavior)
    Ascii, Utf16Le, Utf16Be, Utf32Le, SystemDefault, AutoDetect
}
```

### 2. Encoding Service (✅ COMPLETED)

**File: `Services\EncodingService.cs`**

Key fix: `new UTF8Encoding(false)` instead of `Encoding.UTF8`

```csharp
public static Encoding GetSystemEncoding(FileEncoding fileEncoding)
{
    return fileEncoding switch
    {
        FileEncoding.Utf8NoBom => new UTF8Encoding(false),    // ✅ NO BOM!
        FileEncoding.Utf8WithBom => new UTF8Encoding(true),   // With BOM
        // ... other encodings
    };
}
```

**Features:**
- ✅ **BOM Detection** - Automatically detects file encoding by analyzing byte patterns
- ✅ **Encoding Conversion** - Maps FileEncoding enum to System.Text.Encoding  
- ✅ **Smart Defaults** - UTF-8 without BOM as default
- ✅ **Preserve Original** - Option to maintain existing file encoding

### 3. Updated FileService Methods (✅ COMPLETED)

**File: `Services\FileService.cs` - COMPLETELY REWRITTEN**

All file writing methods now support encoding:

```csharp
// ✅ FIXED: Now supports encoding options
public async Task WriteFileAsync(string path, string content, FileWriteOptions? options = null)
{
    var (_, encoding) = await EncodingService.DetermineEncodingAsync(fullPath, options);
    await File.WriteAllTextAsync(fullPath, content, encoding); // ✅ Uses proper encoding
}

// ✅ FIXED: Edit operations now support encoding  
public async Task<EditResult> EditFileAsync(string path, List<FileEdit> edits, bool dryRun = false, FileWriteOptions? options = null)
{
    var (fileEncoding, encoding) = await EncodingService.DetermineEncodingAsync(fullPath, options);
    var lines = new List<string>(await File.ReadAllLinesAsync(fullPath, encoding));
    // ... edit logic ...
    await File.WriteAllLinesAsync(fullPath, lines, encoding); // ✅ Uses proper encoding
}

// ✅ FIXED: File creation now supports encoding
public async Task CreateFileAsync(string path, string? content = null, FileWriteOptions? options = null)
{
    var (_, encoding) = await EncodingService.DetermineEncodingAsync(fullPath, options);
    await File.WriteAllTextAsync(fullPath, content ?? string.Empty, encoding); // ✅ Uses proper encoding
}
```

### 4. Enhanced MCP Tools (✅ COMPLETED)

**File: `Tools\FileTools.cs` - COMPLETELY REWRITTEN**

#### ✅ Enhanced write_file Tool
```csharp
[McpServerTool("write_file")]
public static async Task<string> WriteFile(
    string path,
    string content,
    string encoding = "utf8",                    // ✅ NEW: Defaults to UTF-8 no BOM
    bool preserveOriginalEncoding = false)       // ✅ NEW: Preserve option
```

#### ✅ Enhanced edit_file Tool  
```csharp
[McpServerTool("edit_file")]
public static async Task<string> EditFile(
    string path,
    string editsJson,
    bool dryRun = false,
    string encoding = "utf8",                    // ✅ NEW: Encoding control
    bool preserveOriginalEncoding = true)        // ✅ NEW: Default preserve for edits
```

#### ✅ NEW Tool: detect_file_encoding
```csharp
[McpServerTool("detect_file_encoding")]
public static async Task<string> DetectFileEncoding(string path)
// Returns detailed encoding information including BOM detection
```

### 5. Encoding Options Reference

| Parameter Value | Result | Use Case |
|-----------------|--------|----------|
| `"utf8"` | UTF-8 without BOM | ✅ **DEFAULT** - Most compatible |
| `"utf8-bom"` | UTF-8 with BOM | Windows applications that expect BOM |
| `"ascii"` | ASCII | Simple text files |
| `"auto"` | Auto-detect | Smart detection for reading |

## Usage Examples

### ✅ Write file without BOM (new default behavior)
```json
{
  "tool": "write_file",
  "path": "test.txt",
  "content": "Hello World"
  // encoding defaults to "utf8" (no BOM)
}
```

### ✅ Write file with BOM (old behavior, now explicit)
```json
{
  "tool": "write_file", 
  "path": "test.txt",
  "content": "Hello World",
  "encoding": "utf8-bom"
}
```

### ✅ Edit file preserving original encoding
```json
{
  "tool": "edit_file",
  "path": "existing.txt", 
  "editsJson": "[{\"LineNumber\":1,\"Type\":0,\"Text\":\"New line\"}]",
  "preserveOriginalEncoding": true
}
```

### ✅ Detect file encoding
```json
{
  "tool": "detect_file_encoding",
  "path": "mystery.txt"
}
```

## Key Benefits Delivered

✅ **Default Fixed** - UTF-8 without BOM is now the default (most compatible)
✅ **Full Control** - Explicit encoding control for all file operations  
✅ **Backward Compatible** - Can still create files with BOM when needed
✅ **Smart Detection** - Auto-detects existing file encodings
✅ **Preserve Option** - Can maintain original encoding when editing
✅ **New Diagnostic Tool** - Can analyze file encodings

## Files Modified/Created

1. ✅ `MCPFileSystem.Contracts\FileWriteOptions.cs` - **NEW** encoding contracts
2. ✅ `Services\EncodingService.cs` - **NEW** encoding utilities  
3. ✅ `Services\FileService.cs` - **COMPLETELY REWRITTEN** with encoding support
4. ✅ `Tools\FileTools.cs` - **COMPLETELY REWRITTEN** with encoding parameters
5. ✅ `MCPFileSystem.Contracts\FileEdit.cs` - Updated with WriteOptions property

## Resolution Status

🎯 **PROBLEM SOLVED**: The server now defaults to UTF-8 **without BOM** for maximum compatibility, while providing full control over encoding for all file operations.

**Before**: All files created with UTF-8 BOM (uncontrollable)  
**After**: Files default to UTF-8 without BOM, with full encoding control available

The solution provides enterprise-grade encoding management while maintaining simplicity for common use cases.
