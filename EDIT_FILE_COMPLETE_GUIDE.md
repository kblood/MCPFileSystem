# Edit File Tool - Complete Usage Guide

## Overview

The `edit_file` tool in MCPFileSystem provides reliable replace-based editing capabilities for text files. This guide provides complete examples with correct JSON formatting to avoid common errors.

## ⚠️ Critical: JSON Format Requirements

The `editsJson` parameter must be a **JSON string** that deserializes to an array of `FileEdit` objects. Only Replace operations are supported for maximum reliability.

## FileEdit Object Structure

```typescript
interface FileEdit {
    LineNumber: number;      // 1-based line number
    Type: string;           // Must be "Replace" (only supported operation)
    Text: string;           // Replacement content
    OldText?: string;       // Optional: specific text to replace within line
}
```

## Edit Operation Type

### Replace - Replace Entire Lines or Text Within Lines

**Purpose**: Replace content in existing lines with more reliable text-based matching.

**Behavior**:
- If `OldText` is null or empty: Replace entire line with `Text`
- If `OldText` is provided: Replace only that specific text within the line

**Required Fields**: `LineNumber`, `Type`, `Text`
**Optional Fields**: `OldText`

### 4. ReplaceSection - Replace Multiple Lines

**Purpose**: Replace a range of lines efficiently.

**Required Fields**: `LineNumber`, `Type`, `Text`, `EndLine`

## Complete JSON Examples

### Example 1: Single Insert Operation

```json
[
  {
    "LineNumber": 5,
    "Type": "Insert", 
    "Text": "// This is a new comment line"
  }
]
```

### Example 2: Replace Multi-line Content

**⚠️ CRITICAL**: Multi-line content must use `\\n` for newlines in JSON!

```json
[
  {
    "LineNumber": 10,
    "Type": "Replace",
    "Text": "function calculateSum(a, b) {\\n    return a + b;\\n}"
  }
]
```

### Example 3: Multiple Replace Operations

```json
[
  {
    "LineNumber": 1,
    "Type": "Replace",
    "Text": "// File header comment"
  },
  {
    "LineNumber": 15,
    "Type": "Replace", 
    "Text": "const maxRetries = 5;"
  },
  {
    "LineNumber": 20,
    "Type": "Replace",
    "Text": "// Updated line content"
  }
]
```

### Example 4: Replace Specific Text Within Line

```json
[
  {
    "LineNumber": 8,
    "Type": "Replace",
    "OldText": "localhost:3000",
    "Text": "production.example.com"
  }
]
```

### Example 5: Complex Multi-line Replacement with Escaping

```json
[
  {
    "LineNumber": 12,
    "Type": "Replace",
    "Text": "const config = {\\n    \"apiUrl\": \"https://api.example.com\",\\n    \"timeout\": 5000\\n};"
  }
]
```

## Common Error Scenarios and Solutions

### ❌ Error 1: Invalid JSON Format

**Bad Example:**
```json
[
  {
    "LineNumber": 5,
    "Type": "REPLACE",  // ❌ Wrong - should be "Replace"
    "Text": "new line"
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Replace",  // ✅ Correct enum value
    "Text": "new line"
  }
]
```

### ❌ Error 2: Incorrect Newline Handling

**Bad Example:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Insert",
    "Text": "line1\nline2"  // ❌ Wrong - literal newlines not allowed in JSON
  }
]
```

### ❌ Error 2: Unescaped Newlines in Multi-line Text

**Bad Example:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Replace",
    "Text": "line1
line2"  // ❌ Invalid JSON - literal newlines not allowed
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Replace", 
    "Text": "line1\\nline2"  // ✅ Escaped newlines
  }
]
```

### ❌ Error 3: Missing Required Fields

**Bad Example:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Replace"
    // ❌ Missing "Text" field required for Replace
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Replace",
    "Text": "new content"  // ✅ Text field provided
  }
]
```

### ❌ Error 4: Unsupported Operation Type

**Bad Example:**
```json
[
  {
    "LineNumber": 10,
    "Type": "Insert",
    "Text": "new content"
    // ❌ Insert operations are no longer supported
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 10,
    "Type": "Replace",  // ✅ Only Replace operations are supported
    "Text": "new content"
  }
]
```

## Escape Sequence Reference

| Character | JSON Escape | Example |
|-----------|-------------|---------|
| Newline | `\\n` | `"line1\\nline2"` |
| Double Quote | `\\"` | `"He said \\"Hello\\""` |
| Backslash | `\\\\` | `"C:\\\\temp\\\\file.txt"` |
| Tab | `\\t` | `"Column1\\tColumn2"` |
| Carriage Return | `\\r` | `"Line1\\r\\nLine2"` |

## Dry Run for Preview

Always test complex edits with `dryRun: true` first:

```json
// Tool call with dry run
{
  "tool": "edit_file",
  "arguments": {
    "path": "src/myfile.js",
    "editsJson": "[{\"LineNumber\":5,\"Type\":\"Replace\",\"Text\":\"// New comment\"}]",
    "dryRun": true
  }
}
```

This will show you a diff preview without making actual changes.

## Real-World Complete Examples

### Example: Updating Error Handling in JavaScript Function

**Original File:**
```javascript
function processData(data) {
    const result = data.map(item => item.value);
    return result;
}
```

**Edit Operation:**
```json
[
  {
    "LineNumber": 2,
    "Type": "Replace",
    "Text": "    if (!data || !Array.isArray(data)) {\\n        throw new Error('Invalid data provided');\\n    }\\n    const result = data.map(item => item.value);"
  }
]
```

**Result:**
```javascript
function processData(data) {
    if (!data || !Array.isArray(data)) {
        throw new Error('Invalid data provided');
    }
    const result = data.map(item => item.value);
    return result;
}
```

### Example: Updating Configuration Object

**Original File:**
```javascript
const config = {
    environment: 'development',
    port: 3000
};
```

**Edit Operations:**
```json
[
  {
    "LineNumber": 2,
    "Type": "Replace",
    "OldText": "development",
    "Text": "production"
  },
  {
    "LineNumber": 3,
    "Type": "Replace", 
    "OldText": "port: 3000",
    "Text": "port: 8080,\\n    database: 'mongodb://localhost:27017/myapp'"
  }
]
```

**Result:**
```javascript
const config = {
    environment: 'production',
    port: 8080,
    database: 'mongodb://localhost:27017/myapp'
};
```

## Testing Your Edit Operations

1. **Start Simple**: Test with single-line replacements first
2. **Use Dry Run**: Always preview complex changes
3. **Validate JSON**: Ensure your JSON is properly formatted
4. **Check Line Numbers**: Verify line numbers are correct (1-based)
5. **Test Incrementally**: Apply edits one by one for complex changes

## Quick Reference: JSON Template

```json
[
  {
    "LineNumber": 1,
    "Type": "Replace",
    "Text": "content with \\n for newlines",
    "OldText": "optional: specific text to replace within line"
  }
]
```

## AI Assistant Guidelines

When generating `editsJson`:

1. ✅ Use only `"Replace"` operation type (other types are not supported)
2. ✅ Escape newlines as `\\n` in JSON strings
3. ✅ Escape quotes as `\\"` in JSON strings  
4. ✅ Include required fields: `LineNumber`, `Type`, `Text`
5. ✅ Use 1-based line numbers
6. ✅ Test with dry run for complex operations
7. ✅ Use `OldText` for precision when replacing specific text within a line

Following these guidelines will ensure successful file editing operations with reliable Replace-based editing.
