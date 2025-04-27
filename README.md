# MCPFileSystem - Model Context Protocol Server

A C# implementation of the Model Context Protocol (MCP) for file system operations, providing LLMs with the ability to:
- Read and write files
- Create and list directories
- Search for files
- Navigate directory trees
- Manage file system paths

## Overview

This project has been updated to use the official [Model Context Protocol (MCP)](https://github.com/modelcontextprotocol/csharp-sdk) SDK, replacing the previous custom TCP-based implementation. This ensures compatibility with LLM platforms that support the MCP standard.

## Components

- **MCPFileSystemServer**: The MCP server that exposes file system operations as tools
- **MCPFileSystemClient**: A client for testing the server (needs updating to match the new protocol)

## Features

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

### Available Tools

The server exposes the following MCP tools:

#### File Tools
- `list_files`: List all files in a directory
- `read_file`: Read file contents
- `write_file`: Write content to a file
- `edit_file`: Make edits to a file
- `move_file`: Move or rename a file
- `get_file_info`: Get file metadata

#### Directory Tools
- `create_directory`: Create a new directory
- `list_directory`: List all files and directories
- `directory_tree`: Get a recursive directory tree
- `search_files`: Search for files matching a pattern
- `list_allowed_directories`: List all directories the server can access

#### Project Tools
- `list_projects`: Find all .csproj files
- `list_solutions`: Find all .sln files
- `list_source_files`: List all source code files

#### Configuration Tools
- `set_base_directory`: Change the base directory
- `get_base_directory`: Get the current base directory

## Implementation Details

The project follows a clean architecture with:
- **Services**: Core business logic for file operations
- **Tools**: MCP-compliant interfaces for the services
- **Models**: Data structures for file system entities

## Security Considerations

The server implements path validation to prevent access to files outside of allowed directories. Use caution when setting the base directory or adding additional accessible directories.
