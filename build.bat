@echo off
echo Building MCPFileSystemServer...

dotnet build MCPFileSystemServer -c Release
if %ERRORLEVEL% neq 0 (
  echo Build failed with error level %ERRORLEVEL%
  exit /b %ERRORLEVEL%
)

dotnet publish MCPFileSystemServer -c Release -o publish --self-contained false
if %ERRORLEVEL% neq 0 (
  echo Publish failed with error level %ERRORLEVEL%
  exit /b %ERRORLEVEL%
)

echo Done! Executable is in the publish directory.
