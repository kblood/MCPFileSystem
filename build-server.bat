@echo off
echo Building MCPFileSystemServer...
dotnet publish -c Release -o ./publish MCPFileSystemServer/MCPFileSystemServer.csproj
echo Done! Executable is in the publish directory.
