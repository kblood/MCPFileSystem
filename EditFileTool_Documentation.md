# Edit File Tool Documentation

## ⚠️ **READ THIS FIRST**

**For complete usage with correct JSON examples, see: [EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)**

This document covers technical implementation details. For practical usage examples and error solutions, use the complete guide above.

## Overview

The `edit_file` tool in the MCPFileSystemServer provides functionality for making line-based edits to text files. This document outlines the technical implementation and best practices for constructing proper JSON for file editing operations.

## Current Tool Signature

```csharp
[McpServerTool("edit_file")]
public static async Task<string> EditFile(
    string path,                    // Path to file to edit
    string editsJson,              // JSON string of edit operations
    bool dryRun = false)           // Preview changes without applying
```

## FileEdit Model Structure

The `editsJson` parameter must deserialize to `List<FileEdit>` with this structure:

```csharp
public class FileEdit
{
    public int LineNumber { get; set; }      // 1-based line number
    public EditType Type { get; set; }       // Insert, Delete, Replace, ReplaceSection
    public string? Text { get; set; }        // Content for Insert/Replace
    public string? OldText { get; set; }     // For targeted Replace operations
    public int? EndLine { get; set; }        // For ReplaceSection operations
}
```

## EditType Enum Values

⚠️ **CRITICAL**: Use exact enum values, not uppercase strings!

```csharp
public enum EditType
{
    Insert,           // NOT "INSERT" 
    Delete,           // NOT "DELETE"
    Replace,          // NOT "REPLACE"
    ReplaceSection    // NOT "REPLACE_SECTION"
}
```

## Operation Types in Detail

### Insert Operations
- **Purpose**: Add new content at specified line
- **Required**: `LineNumber`, `Type`, `Text`
- **Behavior**: 
  - Line 1: Insert before first line
  - Line > total: Append to end
  - Otherwise: Insert before specified line

### Delete Operations  
- **Purpose**: Remove entire lines
- **Required**: `LineNumber`, `Type`
- **Text field**: Ignored

### Replace Operations
- **Purpose**: Replace line content
- **Required**: `LineNumber`, `Type`, `Text`
- **Optional**: `OldText`
- **Behavior**:
  - No `OldText`: Replace entire line
  - With `OldText`: Replace only matching text within line

### ReplaceSection Operations
- **Purpose**: Replace multiple lines efficiently
- **Required**: `LineNumber`, `Type`, `Text`, `EndLine`
- **Behavior**: Replace lines from `LineNumber` to `EndLine` (inclusive)

## JSON Formatting Critical Rules

### 1. Newline Character Handling

**Multi-line content MUST use `\\n` escape sequences in JSON:**

✅ **Correct**:
```json
[{
  "LineNumber": 5,
  "Type": "Insert",
  "Text": "function example() {\\n    console.log('Hello');\\n}"
}]
```

❌ **Wrong** (causes JSON parsing errors):
```json
[{
  "LineNumber": 5,
  "Type": "Insert", 
  "Text": "function example() {
    console.log('Hello');
}"
}]
```

### 2. Quote Escaping

**Literal quotes must be escaped as `\\"` in JSON strings:**

✅ **Correct**:
```json
[{
  "LineNumber": 10,
  "Type": "Insert",
  "Text": "console.log(\\"Hello World\\");"
}]
```

### 3. Backslash Escaping

**Literal backslashes must be escaped as `\\\\`:**

✅ **Correct**:
```json
[{
  "LineNumber": 15,
  "Type": "Insert", 
  "Text": "const path = \\"C:\\\\\\\\temp\\\\\\\\file.txt\\";"
}]
```

## Error Handling and Common Issues

### JSON Parsing Errors

The most common errors are:

1. **Wrong enum values**: Using `"INSERT"` instead of `"Insert"`
2. **Unescaped newlines**: Using literal newlines instead of `\\n`
3. **Missing required fields**: Forgetting `Text` for Insert operations
4. **Invalid JSON syntax**: Malformed JSON structure

### Error Response Format

When errors occur, the tool returns:
```json
{
  "error": "Invalid JSON format for edits: [specific error message]"
}
```

### Successful Response Format

```json
{
  "Success": true,
  "Message": "Applied 3 edits to file successfully",
  "EditCount": 3,
  "Diff": "[optional diff output for dry runs]"
}
```

## Best Practices for AI Assistants

When generating `editsJson` parameters:

1. ✅ **Always use correct enum values**: `"Insert"`, `"Delete"`, `"Replace"`, `"ReplaceSection"`
2. ✅ **Escape newlines properly**: Use `\\n` for multi-line content
3. ✅ **Escape quotes and backslashes**: Follow JSON string escaping rules
4. ✅ **Validate required fields**: Each operation type has required properties
5. ✅ **Use 1-based line numbers**: Line numbering starts at 1, not 0
6. ✅ **Test with dry-run first**: Use `"dryRun": true` for complex operations
7. ✅ **Handle edge cases**: Check file bounds and line existence

## Example Implementation (C# Client)

```csharp
// Proper way to construct edit operations
var operations = new List<FileEdit>
{
    new FileEdit
    {
        LineNumber = 5,
        Type = EditType.Insert,  // Enum value, not string
        Text = "// This is a comment\\nconsole.log(\\"Hello\\");"
    }
};

// Serialize with proper options
var json = JsonSerializer.Serialize(operations, new JsonSerializerOptions 
{ 
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
});

// Call the tool
var result = await editFileTool(filePath, json, dryRun: false);
```

## Testing and Validation

### Pre-flight Checks
1. Validate JSON syntax with a JSON parser
2. Verify enum values match exactly
3. Check that all required fields are present
4. Test line numbers are within file bounds
5. Use dry-run to preview changes

### Integration Testing
```csharp
// Test basic insert
var insertJson = "[{\"LineNumber\":1,\"Type\":\"Insert\",\"Text\":\"// Header\"}]";
var result = await EditFile("test.js", insertJson, true);

// Verify no errors
Assert.DoesNotContain("error", result);

// Check diff output
var response = JsonSerializer.Deserialize<EditResult>(result);
Assert.NotNull(response.Diff);
```

## Related Documentation

- **[EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)** - Complete usage guide with examples
- **[LineEditOperationsExamples.md](./LineEditOperationsExamples.md)** - C# client usage examples
- **[FileEdit.cs](./MCPFileSystem.Contracts/FileEdit.cs)** - Complete model definition
- **[EditResult.cs](./MCPFileSystem.Contracts/EditResult.cs)** - Response model definition

## Technical Implementation Notes

- Uses JSON deserialization with case-insensitive property matching
- Supports both single and batch edit operations
- Implements atomic operations (all-or-nothing for multiple edits)
- Provides detailed error messages for troubleshooting
- Supports dry-run preview with diff output
- Validates file paths against allowed directories
- Handles concurrent access through file locking

The `edit_file` tool provides enterprise-grade file editing capabilities with robust error handling and flexible operation types suitable for automated code generation and file modification workflows.
