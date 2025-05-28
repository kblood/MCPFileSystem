# Edit File Tool - Error Troubleshooting Guide

## 🚨 Common Errors and Solutions

This guide addresses the most frequently encountered errors when using the `edit_file` tool. Note: Only Replace operations are supported.

## Error 1: "Unsupported edit type"

### ❌ Problem: Using Unsupported Operation Types
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "new line"}]
```
**Error**: `Unsupported edit type: Insert. Only Replace operations are supported.`

### ✅ Solution: Use Only Replace Operations
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "new line"}]
```

**Only Supported Value**: `"Replace"`

---

## Error 2: "Invalid JSON format for edits"

### ❌ Problem: Wrong Enum Values
```json
[{"LineNumber": 5, "Type": "REPLACE", "Text": "new line"}]
```
**Error**: `The JSON value could not be converted to EditType`

### ✅ Solution: Use Correct Enum Value
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "new line"}]
```

**Correct Value**: `"Replace"` (case-sensitive)

---

## Error 3: "Unterminated string" JSON Error

### ❌ Problem: Unescaped Newlines
```json
[{
  "LineNumber": 5,
  "Type": "Replace",
  "Text": "line1
line2"
}]
```

### ✅ Solution: Escape Newlines with \\n
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "line1\\nline2"}]
```

---

## Error 4: "Unexpected character" JSON Error

### ❌ Problem: Unescaped Quotes
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "console.log("Hello");"}]
```

### ✅ Solution: Escape Quotes with \\"
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "console.log(\\"Hello\\");"}]
```

---

## Error 5: "Required property missing"

### ❌ Problem: Missing Text Field for Replace
```json
[{"LineNumber": 5, "Type": "Replace"}]
```

### ✅ Solution: Include Required Fields
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "new content"}]
```

**Required Fields for Replace**:
- `LineNumber`: Line number to edit (1-based)
- `Type`: Must be "Replace"
- `Text`: Replacement content

**Optional Fields**:
- `OldText`: Specific text to replace within the line (if omitted, replaces entire line)

---json
[{"LineNumber": 10, "Type": "ReplaceSection", "EndLine": 15, "Text": "new content"}]
```

---

## Error 6: "Line number out of range"

## Error 6: "Invalid Line Numbers"

### ❌ Problem: Invalid Line Numbers
```json
[{"LineNumber": 0, "Type": "Replace", "Text": "new line"}]
```

### ✅ Solution: Use 1-Based Line Numbers
```json
[{"LineNumber": 1, "Type": "Replace", "Text": "new line"}]
```

**Line Number Rules**:
- **Minimum**: 1 (first line)
- **Maximum**: Any number within existing file range for Replace operations
- Line must exist in the file for Replace operations

---

## Quick JSON Validation Checklist

✅ **Before Submitting Your Edit JSON**:

1. **Operation Type**: Only `"Replace"` is supported (exact case)
2. **Newlines**: Use `\\n` for multi-line content
3. **Quotes**: Escape literal quotes as `\\"` 
4. **Backslashes**: Escape as `\\\\` 
5. **Required Fields**: `LineNumber`, `Type`, `Text`
6. **Line Numbers**: Use 1-based numbering for existing lines
7. **JSON Syntax**: Valid JSON array structure
8. **OldText**: Optional field for precise text replacement within line

---

## Debugging Tools

### Test with Simple Operations First
```json
[{"LineNumber": 1, "Type": "Replace", "Text": "simple test"}]
```

### Use Dry Run for Preview
```json
{
  "tool": "edit_file",
  "arguments": {
    "path": "test.txt",
    "editsJson": "[{\"LineNumber\": 1, \"Type\": \"Replace\", \"Text\": \"test\"}]",
    "dryRun": true
  }
}
```

### JSON Validation Online
Use online JSON validators like:
- jsonlint.com
- jsonformatter.curiousconcept.com

---

## Error Message Decoder

| Error Contains | Likely Cause | Solution |
|----------------|--------------|----------|
| "Unsupported edit type" | Wrong operation type | Use only "Replace" |
| "EditType" | Wrong enum value | Check capitalization |
| "Unterminated string" | Unescaped newline | Use `\\n` |
| "Unexpected character" | Unescaped quote | Use `\\"` |
| "Required property" | Missing field | Add required properties |
| "out of range" | Invalid line number | Use 1-based numbers |

---

## Template for Copy-Paste Testing

### Replace Template (Basic)
```json
[{"LineNumber": 1, "Type": "Replace", "Text": "replacement content"}]
```

### Replace Template (With OldText)
```json
[{"LineNumber": 1, "Type": "Replace", "OldText": "old text", "Text": "new text"}]
```

### Multi-line Replace Template  
```json
[{"LineNumber": 1, "Type": "Replace", "Text": "line1\\nline2\\nline3"}]
```

---

## Still Having Issues?

1. **Read the complete guide**: [EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)
2. **Check technical docs**: [EditFileTool_Documentation.md](./EditFileTool_Documentation.md)
3. **Validate your JSON**: Use online JSON validators
4. **Test with dry-run**: Always preview complex operations
5. **Start simple**: Test basic operations before complex ones

**Remember**: Only "Replace" operations are supported. The most common error is using wrong enum values (`"REPLACE"` vs `"Replace"`)!
