@echo off
echo ===============================================
echo MCPFileSystemServer Quick Build & Publish
echo ===============================================

REM Create the target directory if it doesn't exist
if not exist "C:\LLM\MCPTools" (
    echo Creating directory C:\LLM\MCPTools...
    mkdir "C:\LLM\MCPTools"
)

echo Publishing MCPFileSystemServer to C:\LLM\MCPTools...
dotnet publish -c Release -o "C:\LLM\MCPTools" MCPFileSystemServer/MCPFileSystemServer.csproj

if %ERRORLEVEL% neq 0 (
    echo.
    echo Build/Publish failed with error level %ERRORLEVEL%
    echo Try running 'dotnet clean' and 'dotnet restore' first.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ===============================================
echo Done! 
echo ===============================================
echo Executable: C:\LLM\MCPTools\MCPFileSystem\MCPFileSystemServer.exe
echo ===============================================
