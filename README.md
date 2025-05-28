# MCPFileSystem - Model Context Protocol Server

A C# implementation of the Model Context Protocol (MCP) for file system operations, providing LLMs with the ability to:
- Read and write files
- **Advanced line-based file editing** with multiple operation types
- Create and list directories
- Search for files
- Navigate directory trees
- Manage file system paths

## Overview

This project uses the official [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/csharp-sdk) SDK, ensuring full compatibility with LLM platforms that support the MCP standard.

## Components

- **MCPFileSystemServer**: The MCP server that exposes file system operations as tools
- **MCPFileSystemClient**: A client for testing the server (needs updating to match the new protocol)

## Features

### **Advanced File Editing** 🔥
- **Line-based editing** with precise control
- **Multiple operation types**: Insert, Delete, Replace, ReplaceSection
- **Multi-line content support** with proper newline handling
- **Selective text replacement** within specific lines
- **Dry-run preview** with diff output
- **JSON-based operation specification**

### **Standard File Operations**
- **File Operations**: Read, write, edit, and move files
- **Directory Operations**: List, create, navigate directory structures
- **Search Capabilities**: Find files by name or pattern
- **Path Safety**: Validation to ensure operations only work within allowed directories
- **MCP Compliance**: Full compliance with the Model Context Protocol standard

## Getting Started

### Running the Server

```bash
cd MCPFileSystemServer
dotnet run -- --root "C:\YourDirectory" --dir "C:\AnotherAllowedDirectory"
```

Command line options:
- `--root`: Sets the base directory (required)
- `--dir`: Adds additional accessible directories (optional, can be used multiple times)

## Available Tools

### File Tools
- `list_files`: List all files in a directory
- `read_file`: Read file contents
- `write_file`: Write content to a file
- **`edit_file`**: **Advanced line-based file editing** ⭐
- `move_file`: Move or rename a file
- `get_file_info`: Get file metadata

### Directory Tools
- `create_directory`: Create a new directory
- `list_directory`: List all files and directories
- `directory_tree`: Get a recursive directory tree
- `search_files`: Search for files matching a pattern
- `list_allowed_directories`: List all directories the server can access

### Project Tools
- `list_projects`: Find all .csproj files
- `list_solutions`: Find all .sln files
- `list_source_files`: List all source code files
- `get_code_outline`: Extract C# code structure using Roslyn
- `get_code_outlines_for_directory`: Analyze multiple C# files

### Configuration Tools
- `set_base_directory`: Change the base directory
- `get_base_directory`: Get the current base directory

## 📝 File Editing Documentation

The `edit_file` tool is the most powerful feature of MCPFileSystem. For complete usage instructions with JSON examples:

### **📚 Essential Reading**
- **[EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)** - **Complete guide with correct JSON examples**
- [EditFileTool_Documentation.md](./EditFileTool_Documentation.md) - Technical implementation details
- [LineEditOperationsExamples.md](./LineEditOperationsExamples.md) - C# client examples

### **Quick Edit Examples**

#### Replace Entire Line
```json
[{"LineNumber": 15, "Type": "Replace", "Text": "const maxRetries = 5;"}]
```

#### Replace Text Within Line
```json
[{"LineNumber": 8, "Type": "Replace", "OldText": "localhost", "Text": "production.example.com"}]
```

#### Multi-line Replace
```json
[{"LineNumber": 10, "Type": "Replace", "Text": "function test() {\\n    console.log('Hello');\\n}"}]
```

### **⚠️ Critical JSON Rules**
1. **Use proper Type value**: Only `"Replace"` is supported 
2. **Escape newlines**: Use `\\n` for multi-line content
3. **Escape quotes**: Use `\\"` for literal quotes in text
4. **1-based line numbers**: Line numbering starts at 1
5. **Test with dry-run**: Use `"dryRun": true` for complex operations

### **Common Errors and Solutions**

❌ **Wrong enum values** (causes JSON parsing errors):
```json
[{"LineNumber": 5, "Type": "REPLACE", "Text": "new line"}]  // ❌ Wrong
```

✅ **Correct enum values**:
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "new line"}]  // ✅ Correct
```

❌ **Incorrect newline handling** (JSON syntax error):
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "line1\nline2"}]  // ❌ Wrong
```

✅ **Proper newline escaping**:
```json
[{"LineNumber": 5, "Type": "Replace", "Text": "line1\\nline2"}]  // ✅ Correct
```

## Configuration for Claude Desktop

```json
{
  "mcpServers": {
    "filesystem": {
      "command": "path/to/MCPFileSystemServer.exe",
      "args": ["--root", "C:/YourProjectDirectory"]
    }
  }
}
```

## Use with mcpo (OpenAPI Proxy)

MCPFileSystem is fully compatible with [mcpo](https://github.com/open-webui/mcpo):

```bash
# Basic usage
uvx mcpo --port 8000 --api-key "your-key" -- path/to/MCPFileSystemServer.exe --root "C:/Projects"
```

Then access your file tools via REST API at `http://localhost:8000/docs`

## Implementation Details

The project follows a clean architecture with:
- **Services**: Core business logic for file operations
- **Tools**: MCP-compliant interfaces for the services  
- **Models**: Data structures for file system entities
- **Contracts**: Shared models for client-server communication

## Security Considerations

The server implements path validation to prevent access to files outside of allowed directories. Use caution when setting the base directory or adding additional accessible directories.

### Path Security Features
- **Base directory restriction**: All operations must be within allowed paths
- **Path traversal protection**: Prevents `../` attacks
- **Whitelist approach**: Only explicitly allowed directories are accessible
- **Validation on every operation**: Each file/directory access is validated

## Building the Project

```bash
# Build all projects
dotnet build

# Run the server
cd MCPFileSystemServer
dotnet run -- --root "C:\YourDirectory"

# Run tests (if available)
dotnet test
```

## Troubleshooting Edit Operations

If you encounter errors with the `edit_file` tool:

1. **Check JSON syntax**: Validate your JSON using a JSON validator
2. **Verify enum values**: Use exact enum value (`Replace` only)
3. **Test line numbers**: Ensure line numbers exist in the target file
4. **Use dry-run**: Preview changes with `"dryRun": true`
5. **Check escaping**: Ensure proper escaping of quotes and newlines
6. **Read the complete guide**: See [EDIT_FILE_COMPLETE_GUIDE.md](./EDIT_FILE_COMPLETE_GUIDE.md)

## Contributing

Contributions are welcome! Please ensure:
- Tests pass for all changes
- Documentation is updated for new features
- Code follows the established patterns
- Security considerations are addressed

---

**🔥 The `edit_file` tool is the flagship feature** - it provides precise, efficient file editing capabilities that surpass simple read/write operations. Perfect for code generation, refactoring, and automated file modifications!
