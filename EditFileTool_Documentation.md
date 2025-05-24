# Edit File Tool Documentation

## Overview

The `EditFileAsync` method in the `MCPFileSystemClient` provides functionality for making line-based edits to text files. This document outlines best practices for constructing proper JSON for file editing operations, particularly when used by AI assistants like Claude.

## Proper JSON Formatting for Edit Operations

When using the `EditFileAsync` method or similar tools that accept JSON-formatted edit operations, it's critical to properly format the JSON string, especially when dealing with multi-line text content.

### The `editsJson` Parameter Format

The `editsJson` parameter should be a string representing a valid JSON array of edit objects. Each object defines a single edit operation with the following properties:

- `LineNumber`: (integer, 1-based) The line number where the edit should be applied.
- `Type`: (string) The type of edit - one of 'INSERT', 'DELETE', or 'REPLACE'.
- `Text`: (string) The textual content for the edit. Required for 'INSERT' and 'REPLACE' types.

### Critical Formatting Rules for the `Text` Field

1. **Newline Characters**: When the code or text you're inserting/replacing spans multiple lines, each newline **must** be represented as the two-character escape sequence `\n` within the JSON string value of the `Text` field.

   **Example**: To insert two lines:
   ```
   const a = 1;
   console.log(a);
   ```

   The `Text` field in your JSON should be: 
   ```json
   "const a = 1;\nconsole.log(a);"
   ```

2. **Escaping Quotes**: Since the `Text` field is a JSON string, any literal double quotes (`"`) must be escaped as `\"`.

   **Example**: To insert `console.log("Hello");`
   
   The `Text` field should be: 
   ```json
   "console.log(\"Hello\");"
   ```

3. **Escaping Backslashes**: Literal backslashes (`\`) must be escaped as `\\`.

   **Example**: To insert `const path = "C:\\temp";`
   
   The `Text` field should be: 
   ```json
   "const path = \"C:\\\\temp\";"
   ```

### Common Errors to Avoid

- Using backslashes at the end of lines (`\`) as line continuation characters
- Using backslashes followed by spaces (`\ `) instead of `\n` for newlines
- Forgetting to escape quotes or backslashes within the text content

## Example with MCPFileSystem Client

Here's how to properly construct a serialized edit operations JSON for the `EditFileAsync` method:

```csharp
var operations = new List<EditOperation>
{
    new EditOperation
    {
        LineNumber = 5,
        Type = "INSERT",
        Text = "// This is a comment\nconsole.log(\"Hello, World!\");"
    }
};

var result = await client.EditFileAsync("path/to/file.js", operations);
```

The serialized JSON for these operations would be:

```json
[
  {
    "LineNumber": 5,
    "Type": "INSERT",
    "Text": "// This is a comment\nconsole.log(\"Hello, World!\");"
  }
]
```

Note how newlines are represented as `\n` and quotes are escaped with backslashes.

## AI Assistant Guidelines

AI assistants like Claude must be especially careful to:

1. Properly represent newlines with `\n` escape sequences, not with backslashes at line ends
2. Correctly escape all special characters according to JSON string rules
3. Remember that a parameter like `editsJson` is itself a string, which contains JSON that needs to be properly formatted
