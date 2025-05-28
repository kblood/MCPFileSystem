# Replace-Based File Editing Examples

This document provides examples of how to use the replace-based file editing functionality in the MCPFileSystem client, which is compatible with the format expected by the `edit_file` tool. Only Replace operations are supported.

## Basic Usage Example

```csharp
// Create a MCPFileSystemClient
var client = new MCPFileSystemClient("localhost", 8080);

// Create a list of replace edit operations
var operations = new List<LineEditOperation>
{
    // Replace line 5 with a new comment
    new LineEditOperation
    {
        LineNumber = 5,
        Type = "Replace",
        Text = "// This is a new comment line"
    },
    
    // Replace line 10 with new content
    new LineEditOperation
    {
        LineNumber = 10,
        Type = "Replace",
        Text = "const maxRetries = 3;"
    },
    
    // Replace line 15 with updated content
    new LineEditOperation
    {
        LineNumber = 15,
        Type = "Replace",
        Text = "// Updated line content"
    }
};

// Apply the edits
var result = await client.EditFileWithLineOperationsAsync("path/to/file.js", operations);
Console.WriteLine($"Applied {result.EditCount} edits");
```

## Handling Multi-line Text

When working with multi-line text in the `Text` field, make sure to use the `FormatTextForLineEdit` helper method to ensure newlines are properly formatted as `\n`:

```csharp
// For multi-line text, use the FormatTextForLineEdit method
string multiLineCode = @"function calculateSum(a, b) {
    // Add two numbers
    return a + b;
}";

var operations = new List<LineEditOperation>
{
    new LineEditOperation
    {
        LineNumber = 20,
        Type = "Replace",
        Text = MCPFileSystemClient.FormatTextForLineEdit(multiLineCode)
    }
};

// Apply the edits
var result = await client.EditFileWithLineOperationsAsync("path/to/file.js", operations);
```

## Dry Run to Preview Changes

You can use the `dryRun` parameter to preview changes without actually applying them:

```csharp
// Perform a dry run first
var dryRunResult = await client.EditFileWithLineOperationsAsync("path/to/file.js", operations, true);

// Show the diff to the user
Console.WriteLine("Proposed changes:");
Console.WriteLine(dryRunResult.Diff);

// If user confirms, apply the changes for real
var finalResult = await client.EditFileWithLineOperationsAsync("path/to/file.js", operations, false);
```

## Precise Text Replacement

You can also replace specific text within a line using the `OldText` property:

```csharp
var operations = new List<LineEditOperation>
{
    new LineEditOperation
    {
        LineNumber = 10,
        Type = "Replace",
        OldText = "localhost:3000",
        Text = "production.example.com"
    }
};
```

## Important JSON Formatting Rules

When editing files with multi-line content, remember:

1. Only "Replace" operations are supported
2. Newlines must be represented as `\n` in the JSON string for the `Text` field
3. Double quotes (`"`) must be escaped as `\"` 
4. Backslashes (`\`) must be escaped as `\\`

The helper methods in the `MCPFileSystemClient` class handle these escaping rules for you automatically.

## For AI Assistants

When generating JSON for file edits, always ensure that:

1. Only use "Replace" operation type (Insert, Delete, and ReplaceSection are not supported)
2. Newline characters are represented as `\n` within the `Text` field string value
3. All special characters are properly escaped according to JSON string rules
4. The entire `editsJson` parameter is a single well-formed JSON string
