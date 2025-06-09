# Edit File Tool Documentation

## Overview

The `edit_file` tool provides functionality for making simple text replacements in files. Only global text replacements are supported for maximum reliability. The tool requires only three inputs:

- `filename`: The path to the file to edit
- `oldText`: The text to find in the file (the first occurrence will be replaced)
- `text`: The replacement text

## Proper JSON Formatting for Edit Operations

When using the `edit_file` tool or similar APIs, provide a JSON array of edit objects. Each object must have:

- `OldText`: (string, required) The text to find in the file. The first occurrence will be replaced.
- `Text`: (string, required) The replacement content.

### Example

```
[
  {
    "OldText": "console.log('old message');",
    "Text": "console.log('new message');"
  }
]
```

### Notes
- No line numbers or edit types are supported or required.
- Only the first occurrence of `OldText` in the file will be replaced.
- For multi-line content, use `\n` for newlines in JSON.
- For literal quotes, use `\"` in JSON. For literal backslashes, use `\\` in JSON.

## AI Assistant Guidelines
- Only provide `OldText` and `Text` for each edit.
- Do not attempt to specify line numbers or edit types.
- The tool will not accept or process any line-based or type-based instructions.
