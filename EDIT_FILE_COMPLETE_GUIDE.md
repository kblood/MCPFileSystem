# Edit File Tool - Complete Usage Guide

## Overview

The `edit_file` tool in MCPFileSystem provides powerful line-based editing capabilities for text files. This guide provides complete examples with correct JSON formatting to avoid common errors.

## ⚠️ Critical: JSON Format Requirements

The `editsJson` parameter must be a **JSON string** that deserializes to an array of `FileEdit` objects. Each edit operation has specific properties that must match exactly.

## FileEdit Object Structure

```typescript
interface FileEdit {
    LineNumber: number;      // 1-based line number
    Type: string;           // "Insert", "Delete", "Replace", or "ReplaceSection"
    Text?: string;          // Content for Insert/Replace operations
    OldText?: string;       // For Replace: specific text to replace within line
    EndLine?: number;       // For ReplaceSection: ending line number
}
```

## Edit Operation Types

### 1. Insert - Add New Lines

**Purpose**: Insert new content at a specific line position.

**Behavior**:
- `LineNumber = 1`: Inserts before the first line
- `LineNumber > total lines`: Appends to end of file  
- Otherwise: Inserts before the specified line

**Required Fields**: `LineNumber`, `Type`, `Text`

### 2. Delete - Remove Lines

**Purpose**: Delete entire lines from the file.

**Required Fields**: `LineNumber`, `Type`

### 3. Replace - Replace Entire Lines or Text Within Lines

**Purpose**: Replace content in existing lines.

**Behavior**:
- If `OldText` is null: Replace entire line with `Text`
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

### Example 2: Insert Multi-line Content

**⚠️ CRITICAL**: Multi-line content must use `\\n` for newlines in JSON!

```json
[
  {
    "LineNumber": 10,
    "Type": "Insert",
    "Text": "function calculateSum(a, b) {\\n    return a + b;\\n}"
  }
]
```

### Example 3: Multiple Operations

```json
[
  {
    "LineNumber": 1,
    "Type": "Insert",
    "Text": "// File header comment"
  },
  {
    "LineNumber": 15,
    "Type": "Replace", 
    "Text": "const maxRetries = 5;"
  },
  {
    "LineNumber": 20,
    "Type": "Delete"
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

### Example 5: Replace Section (Multiple Lines)

```json
[
  {
    "LineNumber": 25,
    "Type": "ReplaceSection",
    "EndLine": 30,
    "Text": "// New implementation\\nconst newFunction = () => {\\n    console.log('Updated');\\n};"
  }
]
```

### Example 6: Complex Multi-line with Escaping

```json
[
  {
    "LineNumber": 12,
    "Type": "Insert",
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
    "Type": "INSERT",  // ❌ Wrong - should be "Insert"
    "Text": "new line"
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Insert",  // ✅ Correct enum value
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

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Insert", 
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
    "Type": "Insert"
    // ❌ Missing "Text" field required for Insert
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 5,
    "Type": "Insert",
    "Text": "new content"  // ✅ Text field provided
  }
]
```

### ❌ Error 4: Incorrect ReplaceSection Usage

**Bad Example:**
```json
[
  {
    "LineNumber": 10,
    "Type": "ReplaceSection",
    "Text": "new content"
    // ❌ Missing required "EndLine" field
  }
]
```

**✅ Correct:**
```json
[
  {
    "LineNumber": 10,
    "Type": "ReplaceSection",
    "EndLine": 15,  // ✅ EndLine specified
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
    "editsJson": "[{\"LineNumber\":5,\"Type\":\"Insert\",\"Text\":\"// New comment\"}]",
    "dryRun": true
  }
}
```

This will show you a diff preview without making actual changes.

## Real-World Complete Examples

### Example: Adding Error Handling to JavaScript Function

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
    "Type": "Insert",
    "Text": "    if (!data || !Array.isArray(data)) {\\n        throw new Error('Invalid data provided');\\n    }"
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
    "OldText": "3000",
    "Text": "8080"
  },
  {
    "LineNumber": 3,
    "Type": "Insert",
    "Text": "    database: 'mongodb://localhost:27017/myapp',"
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

1. **Start Simple**: Test with single-line insertions first
2. **Use Dry Run**: Always preview complex changes
3. **Validate JSON**: Ensure your JSON is properly formatted
4. **Check Line Numbers**: Verify line numbers are correct (1-based)
5. **Test Incrementally**: Apply edits one by one for complex changes

## Quick Reference: JSON Template

```json
[
  {
    "LineNumber": 1,
    "Type": "Insert|Delete|Replace|ReplaceSection",
    "Text": "content with \\n for newlines",
    "OldText": "optional: text to replace within line",
    "EndLine": "optional: for ReplaceSection only"
  }
]
```

## AI Assistant Guidelines

When generating `editsJson`:

1. ✅ Use proper enum values: `"Insert"`, `"Delete"`, `"Replace"`, `"ReplaceSection"`
2. ✅ Escape newlines as `\\n` in JSON strings
3. ✅ Escape quotes as `\\"` in JSON strings  
4. ✅ Include all required fields for each operation type
5. ✅ Use 1-based line numbers
6. ✅ Test with dry run for complex operations

Following these guidelines will ensure successful file editing operations without JSON parsing errors.
