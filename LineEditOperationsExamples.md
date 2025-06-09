# Replace-Based File Editing Examples

This document provides examples of how to use the replace-based file editing functionality in the MCPFileSystem client, which is compatible with the format expected by the `edit_file` tool. Only simple text replacements are supported.

## Basic Usage Example

```csharp
// Create a MCPFileSystemClient
var client = new MCPFileSystemClient("localhost", 8080);

// Create a list of replace edit operations
var operations = new List<FileEdit>
{
    // Replace the first occurrence of the old text with the new text
    new FileEdit
    {
        OldText = "console.log('old message');",
        Text = "console.log('new message');"
    }
};

// Apply the edits
var result = await client.EditFileAsync("path/to/file.js", operations);
Console.WriteLine($"Applied {result.EditCount} edits");
```

## Multi-line Text Example

```csharp
var operations = new List<FileEdit>
{
    new FileEdit
    {
        OldText = "function test() {\n    // old code\n}",
        Text = "function test() {\n    // new code\n}"
    }
};
```

## Important JSON Formatting Rules

- Only `OldText` and `Text` are supported for each edit.
- Newlines must be represented as `\n` in the JSON string for the `Text` field.
- Double quotes (`"`) must be escaped as `\"`.
- Backslashes (`\`) must be escaped as `\\`.

## For AI Assistants

- Only use `OldText` and `Text` in the JSON for file edits.
- Do not use line numbers or edit types.
- The tool will only replace the first occurrence of `OldText` with `Text` in the file.
