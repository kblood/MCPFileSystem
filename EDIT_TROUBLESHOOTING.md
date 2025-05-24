# Edit File Tool - Error Troubleshooting Guide

## 🚨 Common Errors and Solutions

This guide addresses the most frequently encountered errors when using the `edit_file` tool.

## Error 1: "Invalid JSON format for edits"

### ❌ Problem: Wrong Enum Values
```json
[{"LineNumber": 5, "Type": "INSERT", "Text": "new line"}]
```
**Error**: `The JSON value could not be converted to EditType`

### ✅ Solution: Use Correct Enum Values
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "new line"}]
```

**Correct Values**: `"Insert"`, `"Delete"`, `"Replace"`, `"ReplaceSection"`

---

## Error 2: "Unterminated string" JSON Error

### ❌ Problem: Unescaped Newlines
```json
[{
  "LineNumber": 5,
  "Type": "Insert",
  "Text": "line1
line2"
}]
```

### ✅ Solution: Escape Newlines with \\n
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "line1\\nline2"}]
```

---

## Error 3: "Unexpected character" JSON Error

### ❌ Problem: Unescaped Quotes
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "console.log("Hello");"}]
```

### ✅ Solution: Escape Quotes with \\"
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "console.log(\\"Hello\\");"}]
```

---

## Error 4: "Required property missing"

### ❌ Problem: Missing Text Field for Insert
```json
[{"LineNumber": 5, "Type": "Insert"}]
```

### ✅ Solution: Include Required Fields
```json
[{"LineNumber": 5, "Type": "Insert", "Text": "new content"}]
```

**Required Fields by Type**:
- **Insert**: `LineNumber`, `Type`, `Text`
- **Delete**: `LineNumber`, `Type`
- **Replace**: `LineNumber`, `Type`, `Text`
- **ReplaceSection**: `LineNumber`, `Type`, `Text`, `EndLine`

---

## Error 5: "EndLine must be specified for ReplaceSection"

### ❌ Problem: Missing EndLine for ReplaceSection
```json
[{"LineNumber": 10, "Type": "ReplaceSection", "Text": "new content"}]
```

### ✅ Solution: Include EndLine
```json
[{"LineNumber": 10, "Type": "ReplaceSection", "EndLine": 15, "Text": "new content"}]
```

---

## Error 6: "Line number out of range"

### ❌ Problem: Invalid Line Numbers
```json
[{"LineNumber": 0, "Type": "Insert", "Text": "new line"}]
```

### ✅ Solution: Use 1-Based Line Numbers
```json
[{"LineNumber": 1, "Type": "Insert", "Text": "new line"}]
```

**Line Number Rules**:
- **Minimum**: 1 (first line)
- **Maximum**: Any number (automatically appends to end if beyond file)
- **Delete operations**: Must be within existing file range

---

## Quick JSON Validation Checklist

✅ **Before Submitting Your Edit JSON**:

1. **Enum Values**: `"Insert"`, `"Delete"`, `"Replace"`, `"ReplaceSection"` (exact case)
2. **Newlines**: Use `\\n` for multi-line content
3. **Quotes**: Escape literal quotes as `\\"` 
4. **Backslashes**: Escape as `\\\\` 
5. **Required Fields**: Include all required properties per operation type
6. **Line Numbers**: Use 1-based numbering
7. **JSON Syntax**: Valid JSON array structure
8. **EndLine**: Required for `"ReplaceSection"` operations

---

## Debugging Tools

### Test with Simple Operations First
```json
[{"LineNumber": 1, "Type": "Insert", "Text": "simple test"}]
```

### Use Dry Run for Preview
```json
{
  "tool": "edit_file",
  "arguments": {
    "path": "test.txt",
    "editsJson": "[{\"LineNumber\": 1, \"Type\": \"Insert\", \"Text\": \"test\"}]",
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
| "EditType" | Wrong enum value | Check capitalization |
| "Unterminated string" | Unescaped newline | Use `\\n` |
| "Unexpected character" | Unescaped quote | Use `\\"` |
| "Required property" | Missing field | Add required properties |
| "out of range" | Invalid line number | Use 1-based numbers |
| "EndLine must be" | ReplaceSection missing EndLine | Add EndLine property |

---

## Template for Copy-Paste Testing

### Insert Template
```json
[{"LineNumber": 1, "Type": "Insert", "Text": "your content here"}]
```

### Multi-line Insert Template  
```json
[{"LineNumber": 1, "Type": "Insert", "Text": "line1\\nline2\\nline3"}]
```

### Replace Template
```json
[{"LineNumber": 1, "Type": "Replace", "Text": "replacement content"}]
```

### Delete Template
```json
[{"LineNumber": 1, "Type": "Delete"}]
```

### ReplaceSection Template
```json
[{"LineNumber": 1, "Type": "ReplaceSection", "EndLine": 3, "Text": "new section content"}]
```

---

## Still Having Issues?

1. **Read the complete guide**: [EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)
2. **Check technical docs**: [EditFileTool_Documentation.md](./EditFileTool_Documentation.md)
3. **Validate your JSON**: Use online JSON validators
4. **Test with dry-run**: Always preview complex operations
5. **Start simple**: Test basic operations before complex ones

**Remember**: The most common error is using wrong enum values (`"INSERT"` vs `"Insert"`)!
