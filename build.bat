@echo off
echo Building MCPFileSystemServer...

REM Create the target directory if it doesn't exist
if not exist "C:\LLM\MCPTools" (
    echo Creating directory C:\LLM\MCPTools...
    mkdir "C:\LLM\MCPTools"
)

echo Building Release configuration...
dotnet build MCPFileSystemServer -c Release
if %ERRORLEVEL% neq 0 (
  echo Build failed with error level %ERRORLEVEL%
  exit /b %ERRORLEVEL%
)

echo Publishing to C:\LLM\MCPTools...
dotnet publish MCPFileSystemServer -c Release -o "C:\LLM\MCPTools" --self-contained false
if %ERRORLEVEL% neq 0 (
  echo Publish failed with error level %ERRORLEVEL%
  exit /b %ERRORLEVEL%
)

echo.
echo Build and Publish Complete!
echo Executable location: C:\LLM\MCPTools\MCPFileSystemServer.exe
echo.
