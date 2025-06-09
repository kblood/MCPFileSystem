using MCPFileSystem.Contracts;
using MCPFileSystemServer.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCPFileSystemServer.Services
{
    public class FileService
    {
        private readonly string _basePath;

        public FileService(string basePath)
        {
            _basePath = Path.GetFullPath(basePath);
            // Ensure the base directory exists, creating it if necessary.
            // This is important because FileValidationService.SetBaseDirectory expects an existing directory.
            if (!Directory.Exists(_basePath))
            {
                try
                {
                    Directory.CreateDirectory(_basePath);
                }
                catch (Exception ex)
                {
                    // Consider logging this exception or handling it more gracefully.
                    // For now, rethrow if critical for server startup.
                    throw new InvalidOperationException($"Failed to create base directory '{_basePath}'.", ex);
                }
            }
            FileValidationService.SetBaseDirectory(_basePath); 
        }

        private string GetValidatedFullPath(string relativeOrAbsolutePath) // Renamed parameter for clarity
        {
            // NormalizePath will handle making it absolute if it's relative to BaseDirectory,
            // or validate it if it's already absolute.
            return FileValidationService.NormalizePath(relativeOrAbsolutePath);
        }

        public async Task<PathInfo> GetPathInfoAsync(string path) // Made async
        {
            return await Task.Run(() => 
            {
                string fullPath = GetValidatedFullPath(path);
                bool exists = File.Exists(fullPath) || Directory.Exists(fullPath);
                bool isDirectory = exists && Directory.Exists(fullPath);
                bool isFile = exists && File.Exists(fullPath);

                return new PathInfo
                {
                    Path = path, 
                    Exists = exists,
                    IsDirectory = isDirectory,
                    IsFile = isFile,
                    Type = isDirectory ? "directory" : (isFile ? "file" : "unknown"),
                    IsReadOnly = isFile && new System.IO.FileInfo(fullPath).IsReadOnly,
                };
            });
        }

        public async Task<FileInfoContract> GetFileInfoAsync(string path) // Made async
        {
            return await Task.Run(() =>
            {
                try
                {
                    string fullPath = GetValidatedFullPath(path);
                    if (!File.Exists(fullPath))
                    {
                        return new FileInfoContract { Name = Path.GetFileName(path), FullName = path, Exists = false, ErrorMessage = "File not found." };
                    }

                    var fileInfo = new System.IO.FileInfo(fullPath);
                    return new FileInfoContract
                    {
                        Name = fileInfo.Name,
                        FullName = path,
                        Length = fileInfo.Length,
                        CreationTime = fileInfo.CreationTimeUtc,
                        LastAccessTime = fileInfo.LastAccessTimeUtc,
                        LastWriteTime = fileInfo.LastWriteTimeUtc,
                        IsReadOnly = fileInfo.IsReadOnly,
                        Exists = true,
                        Extension = fileInfo.Extension,
                        DirectoryName = Path.GetDirectoryName(path) // Relative directory name
                    };
                }
                catch (Exception ex)
                {
                    return new FileInfoContract { Name = Path.GetFileName(path), FullName = path, Exists = false, ErrorMessage = ex.Message };
                }
            });
        }

        public async Task<DirectoryInfoContract> GetDirectoryInfoAsync(string path) // Made async
        {
            return await Task.Run(() =>
            {
                try
                {
                    string fullPath = GetValidatedFullPath(path);
                    if (!Directory.Exists(fullPath))
                    {
                        return new DirectoryInfoContract { Name = Path.GetFileName(path), FullName = path, Exists = false, ErrorMessage = "Directory not found." };
                    }

                    var dirInfo = new System.IO.DirectoryInfo(fullPath);
                    return new DirectoryInfoContract
                    {
                        Name = dirInfo.Name,
                        FullName = path, 
                        CreationTime = dirInfo.CreationTimeUtc,
                        LastAccessTime = dirInfo.LastAccessTimeUtc,
                        LastWriteTime = dirInfo.LastWriteTimeUtc,
                        Exists = true,
                        ParentDirectory = Path.GetDirectoryName(path), // Relative parent path
                        RootDirectory = Path.GetPathRoot(fullPath) 
                    };
                }
                catch (Exception ex)
                {
                    return new DirectoryInfoContract { Name = Path.GetFileName(path), FullName = path, Exists = false, ErrorMessage = ex.Message };
                }
            });
        }
        
        // Updated GetDirectoryTreeAsync to match FileTools expectations
        public async Task<DirectoryTreeNode> GetDirectoryTreeAsync(string path, bool respectGitignore, int maxDepth = 3, bool includeFiles = true, int currentDepth = 0)
        {
            return await Task.Run(() =>
            {
                string fullPath = GetValidatedFullPath(path);
                var dirInfoSystem = new System.IO.DirectoryInfo(fullPath);
                string relativePath = path;

                if (!dirInfoSystem.Exists || currentDepth >= maxDepth)
                {
                    return new DirectoryTreeNode
                    {
                        Name = dirInfoSystem.Name,
                        Path = relativePath,
                        Type = dirInfoSystem.Exists ? "directory-depth-limit" : "directory-error",
                        Children = new List<DirectoryTreeNode>()
                    };
                }

                var node = new DirectoryTreeNode
                {
                    Name = dirInfoSystem.Name,
                    Path = relativePath,
                    Type = "directory",
                    Children = new List<DirectoryTreeNode>()
                };

                List<GitignoreRule> gitignoreRules = respectGitignore ? GitignoreService.LoadGitignoreRules(fullPath) : new List<GitignoreRule>();

                try
                {
                    foreach (var subDir in dirInfoSystem.GetDirectories().Where(d => !respectGitignore || !GitignoreService.IsPathIgnored(d.FullName, true, gitignoreRules)))
                    {
                        // Recursively call with async version, ensuring parameters are passed correctly
                        // Note: This recursive call inside Task.Run might lead to nested tasks.
                        // For deep recursion, consider a non-Task.Run approach or iterative method.
                        node.Children.Add(GetDirectoryTreeAsync(Path.Combine(relativePath, subDir.Name), respectGitignore, maxDepth, includeFiles, currentDepth + 1).Result); // .Result is okay inside Task.Run if careful
                    }

                    if (includeFiles)
                    {
                        foreach (var file in dirInfoSystem.GetFiles().Where(f => !respectGitignore || !GitignoreService.IsPathIgnored(f.FullName, false, gitignoreRules)))
                        {
                            node.Children.Add(new DirectoryTreeNode
                            {
                                Name = file.Name,
                                Path = Path.Combine(relativePath, file.Name),
                                Type = "file",
                                Size = file.Length
                            });
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    node.Type = "directory-unauthorized";
                }                catch (Exception ex)
                {
                    node.Type = "directory-error";
                    // Optionally log ex.Message or add to node
                    // For now, we acknowledge the variable 'ex' is captured but not directly used in this simplified error handling.
                    _ = ex; // Suppress unused variable warning
                }
                return node;
            });
        }

        // Updated ListDirectoryContentsAsync to match FileTools expectations
        public async Task<IEnumerable<FileSystemEntry>> ListDirectoryContentsAsync(string path, bool respectGitignore)
        {
            return await Task.Run(() =>
            {
                string fullPath = GetValidatedFullPath(path);
                var directoryInfoSystem = new System.IO.DirectoryInfo(fullPath);
                string relativePath = path;

                if (!directoryInfoSystem.Exists)
                {
                    // Consider throwing FileNotFoundException or DirectoryNotFoundException
                    // return Enumerable.Empty<FileSystemEntry>(); 
                    throw new DirectoryNotFoundException($"Directory not found: {path}");
                }

                var entries = new List<FileSystemEntry>();
                List<GitignoreRule> gitignoreRules = respectGitignore ? GitignoreService.LoadGitignoreRules(fullPath) : new List<GitignoreRule>();

                foreach (var dir in directoryInfoSystem.GetDirectories().Where(d => !respectGitignore || !GitignoreService.IsPathIgnored(d.FullName, true, gitignoreRules)))
                {
                    entries.Add(new FileSystemEntry
                    {
                        Name = dir.Name,
                        Path = Path.Combine(relativePath, dir.Name),
                        IsDirectory = true,
                        Type = "directory",
                        Size = 0,
                        Created = dir.CreationTimeUtc,
                        LastModified = dir.LastWriteTimeUtc
                    });
                }

                foreach (var file in directoryInfoSystem.GetFiles().Where(f => !respectGitignore || !GitignoreService.IsPathIgnored(f.FullName, false, gitignoreRules)))
                {
                    entries.Add(new FileSystemEntry
                    {
                        Name = file.Name,
                        Path = Path.Combine(relativePath, file.Name),
                        IsDirectory = false,
                        Type = "file",
                        Size = file.Length,
                        Created = file.CreationTimeUtc,
                        LastModified = file.LastWriteTimeUtc
                    });
                }
                return entries.AsEnumerable(); // Ensure it returns IEnumerable
            });        }
        
        private async Task<string> ComputeFileSHAAsync(string filePath) // Made async
        {
            try
            {
                using (var stream = File.OpenRead(filePath))
                using (var sha256 = System.Security.Cryptography.SHA256.Create())
                {
                    byte[] hash = await sha256.ComputeHashAsync(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch
            {
                return string.Empty; 
            }        }

        // WriteFileAsync remains largely the same
        public async Task WriteFileAsync(string path, string content)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath))
            {
                throw new UnauthorizedAccessException("File path is invalid or write access is denied.");
            }
            // Ensure directory exists before writing
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);
        }

        // Enhanced WriteFileAsync with encoding options
        public async Task WriteFileAsync(string path, string content, FileWriteOptions options)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath))
            {
                throw new UnauthorizedAccessException("File path is invalid or write access is denied.");
            }

            // Ensure directory exists before writing
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Determine the encoding to use
            Encoding encodingToUse;
            if (options.PreserveOriginalEncoding && File.Exists(fullPath))
            {
                // Detect and preserve the original encoding
                var detectedEncoding = await EncodingUtility.DetectFileEncodingAsync(fullPath);
                encodingToUse = EncodingUtility.ToSystemEncoding(detectedEncoding);
            }
            else if (options.Encoding == FileEncoding.AutoDetect)
            {
                // For auto-detect mode when writing, use UTF-8 without BOM as default
                encodingToUse = EncodingUtility.ToSystemEncoding(FileEncoding.Utf8NoBom);
            }
            else
            {
                // Use the specified encoding
                encodingToUse = EncodingUtility.ToSystemEncoding(options.Encoding);
            }

            await File.WriteAllTextAsync(fullPath, content, encodingToUse);
        }

        // Enhanced ReadFileContentAsync with encoding detection
        public async Task<string> ReadFileContentAsync(string path, FileEncoding? forceEncoding = null)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                throw new FileNotFoundException("File not found or access denied.", path);
            }

            Encoding encodingToUse;
            if (forceEncoding.HasValue && forceEncoding.Value != FileEncoding.AutoDetect)
            {
                // Use the specified encoding
                encodingToUse = EncodingUtility.ToSystemEncoding(forceEncoding.Value);
            }
            else
            {
                // Auto-detect encoding
                var detectedEncoding = await EncodingUtility.DetectFileEncodingAsync(fullPath);
                encodingToUse = EncodingUtility.ToSystemEncoding(detectedEncoding);
            }

            return await File.ReadAllTextAsync(fullPath, encodingToUse);
        }

        // Enhanced ReadFileAsync with encoding detection
        public async Task<ReadFileResponse> ReadFileAsync(string path, int? startLine = null, int? endLine = null, FileEncoding? forceEncoding = null)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                return new ReadFileResponse { FilePath = path, ErrorMessage = "File path is invalid, not found, or access is denied.", Lines = Array.Empty<string>() };
            }

            try
            {
                Encoding encodingToUse;
                FileEncoding detectedEncoding = FileEncoding.Utf8NoBom;
                
                if (forceEncoding.HasValue && forceEncoding.Value != FileEncoding.AutoDetect)
                {
                    // Use the specified encoding
                    encodingToUse = EncodingUtility.ToSystemEncoding(forceEncoding.Value);
                    detectedEncoding = forceEncoding.Value;
                }
                else
                {
                    // Auto-detect encoding
                    detectedEncoding = await EncodingUtility.DetectFileEncodingAsync(fullPath);
                    encodingToUse = EncodingUtility.ToSystemEncoding(detectedEncoding);
                }                var lines = await File.ReadAllLinesAsync(fullPath, encodingToUse);
                int actualStartLine = startLine.HasValue ? Math.Max(0, startLine.Value - 1) : 0; // 0-based for Skip/Take
                int actualEndLine = endLine.HasValue ? Math.Min(lines.Length - 1, endLine.Value - 1) : lines.Length - 1; // 0-based for Skip/Take
                
                if (actualStartLine > actualEndLine || actualStartLine >= lines.Length)
                {
                    return new ReadFileResponse 
                    { 
                        FilePath = path, 
                        Lines = Array.Empty<string>(), 
                        StartLine = startLine ?? 1, 
                        EndLine = endLine ?? lines.Length, 
                        TotalLines = lines.Length,
                        Encoding = detectedEncoding.ToString()
                    };
                }

                var selectedLines = lines.Skip(actualStartLine).Take(actualEndLine - actualStartLine + 1).ToArray();

                return new ReadFileResponse
                {
                    FilePath = path,
                    Lines = selectedLines,
                    StartLine = actualStartLine + 1, // Return 1-based
                    EndLine = actualEndLine + 1,   // Return 1-based
                    TotalLines = lines.Length,
                    FileSHA = await ComputeFileSHAAsync(fullPath),
                    Encoding = detectedEncoding.ToString()
                };
            }
            catch (Exception ex)
            {
                return new ReadFileResponse { FilePath = path, ErrorMessage = $"Error reading file: {ex.Message}", Lines = Array.Empty<string>() };
            }
        }        // Updated EditFileAsync to only support simple text replacement
        public async Task<EditResult> EditFileAsync(string path, List<FileEdit> edits, bool dryRun = false)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                return new EditResult { Success = false, Message = "File path is invalid, not found, or write access is denied." };
            }

            try
            {
                // Validate all edits before processing
                foreach (var edit in edits)
                {
                    edit.NormalizeText();
                    var validation = edit.Validate();
                    if (!validation.IsValid)
                    {
                        return new EditResult
                        {
                            Success = false,
                            Message = $"Edit validation failed: {validation.GetErrorMessage()}"
                        };
                    }
                }

                string content = await File.ReadAllTextAsync(fullPath);
                string originalContent = content;
                int editCount = 0;

                foreach (var edit in edits)
                {
                    if (string.IsNullOrEmpty(edit.OldText)) continue;
                    int idx = content.IndexOf(edit.OldText);
                    if (idx >= 0)
                    {
                        content = content.Remove(idx, edit.OldText.Length).Insert(idx, edit.Text ?? string.Empty);
                        editCount++;
                    }
                }

                if (dryRun)
                {
                    string diff = $"--- Original\n+++ Modified\n";
                    if (originalContent != content)
                    {
                        diff += $"-{originalContent.Replace(Environment.NewLine, "\n-")}\n+{content.Replace(Environment.NewLine, "\n+")}";
                    }
                    else
                    {
                        diff += "No changes.";
                    }
                    return new EditResult { Success = true, Message = "Dry run completed.", Diff = diff, EditCount = editCount };
                }
                else
                {
                    await File.WriteAllTextAsync(fullPath, content);
                    return new EditResult { Success = true, Message = "File edited successfully.", NewFileSHA = await ComputeFileSHAAsync(fullPath), EditCount = editCount };
                }
            }
            catch (Exception ex)
            {
                return new EditResult { Success = false, Message = $"Error editing file: {ex.Message}" };
            }
        }        // Enhanced EditFileAsync with encoding preservation - only supports Replace operations
        public async Task<EditResult> EditFileAsync(string path, List<FileEdit> edits, bool dryRun = false, bool preserveEncoding = true)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath) || !File.Exists(fullPath))
            {
                return new EditResult { Success = false, Message = "File path is invalid, not found, or write access is denied." };
            }

            try
            {
                // Validate all edits before processing
                foreach (var edit in edits)
                {
                    edit.NormalizeText();
                    var validation = edit.Validate();
                    if (!validation.IsValid)
                    {
                        return new EditResult 
                        { 
                            Success = false, 
                            Message = $"Edit validation failed: {validation.GetErrorMessage()}" 
                        };
                    }
                }

                // Detect original encoding if preserving
                FileEncoding originalEncoding = FileEncoding.Utf8NoBom;
                Encoding encodingToUse = Encoding.UTF8;
                
                if (preserveEncoding)
                {
                    originalEncoding = await EncodingUtility.DetectFileEncodingAsync(fullPath);
                    encodingToUse = EncodingUtility.ToSystemEncoding(originalEncoding);
                }
                string content = await File.ReadAllTextAsync(fullPath, encodingToUse);
                string originalContent = content;
                int editCount = 0;

                foreach (var edit in edits)
                {
                    if (string.IsNullOrEmpty(edit.OldText)) continue;
                    int idx = content.IndexOf(edit.OldText);
                    if (idx >= 0)
                    {
                        content = content.Remove(idx, edit.OldText.Length).Insert(idx, edit.Text ?? string.Empty);
                        editCount++;
                    }
                }

                if (dryRun)
                {
                    string diff = $"--- Original\n+++ Modified\n";
                    if (originalContent != content) {
                        diff += $"-{originalContent.Replace(Environment.NewLine, "\n-")}\n+{content.Replace(Environment.NewLine, "\n+")}";
                    } else {
                        diff += "No changes.";
                    }
                    return new EditResult { 
                        Success = true, 
                        Message = "Dry run completed.", 
                        Diff = diff, 
                        EditCount = editCount,
                        PreservedEncoding = preserveEncoding ? originalEncoding.ToString() : null
                    };
                }
                else
                {
                    await File.WriteAllTextAsync(fullPath, content, encodingToUse);
                    return new EditResult { 
                        Success = true, 
                        Message = "File edited successfully.", 
                        NewFileSHA = await ComputeFileSHAAsync(fullPath), 
                        EditCount = editCount,
                        PreservedEncoding = preserveEncoding ? originalEncoding.ToString() : null
                    };
                }
            }
            catch (Exception ex)
            {
                return new EditResult { Success = false, Message = $"Error editing file: {ex.Message}" };
            }
        }

        // Updated CreateDirectoryAsync
        public async Task<DirectoryInfoContract> CreateDirectoryAsync(string path)
        {
            // No Task.Run needed if the operations are already I/O bound and we can use await.
            // However, Directory.CreateDirectory is synchronous.
            // To make this truly async if it were a long operation, Task.Run is appropriate.
            // For now, let's keep Task.Run to match the pattern of other methods, but acknowledge it's not truly async here.
            return await Task.Run(() => 
            {
                string fullPath = GetValidatedFullPath(path);
                if (!FileValidationService.IsPathSafe(fullPath))
                {
                    throw new UnauthorizedAccessException("Directory path is invalid or write access is denied.");
                }
                System.IO.Directory.CreateDirectory(fullPath); // Corrected to System.IO.Directory
                var dirInfo = new System.IO.DirectoryInfo(fullPath);
                return new DirectoryInfoContract 
                {
                    Name = dirInfo.Name,
                    FullName = path, 
                    CreationTime = dirInfo.CreationTimeUtc,
                    LastAccessTime = dirInfo.LastAccessTimeUtc,
                    LastWriteTime = dirInfo.LastWriteTimeUtc,
                    Exists = true,
                    ParentDirectory = Path.GetDirectoryName(path),
                    RootDirectory = Path.GetPathRoot(fullPath)
                };
            });
        }

        // CreateFileAsync (new, if needed, or ensure WriteFileAsync covers creation)
        public async Task CreateFileAsync(string path, string? content = null)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath))
            {
                throw new UnauthorizedAccessException("File path is invalid or write access is denied.");
            }
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            await File.WriteAllTextAsync(fullPath, content ?? string.Empty, Encoding.UTF8);
        }

        // Enhanced CreateFileAsync with encoding options
        public async Task CreateFileAsync(string path, string? content = null, FileWriteOptions? options = null)
        {
            string fullPath = GetValidatedFullPath(path);
            if (!FileValidationService.IsPathSafe(fullPath))
            {
                throw new UnauthorizedAccessException("File path is invalid or write access is denied.");
            }
            var directory = Path.GetDirectoryName(fullPath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (options != null)
            {
                // Use encoding-aware WriteFileAsync
                await WriteFileAsync(path, content ?? string.Empty, options);
            }
            else
            {
                await File.WriteAllTextAsync(fullPath, content ?? string.Empty, Encoding.UTF8);
            }
        }

        // Updated DeletePathAsync
        public async Task DeletePathAsync(string path, bool recursive)
        {
            await Task.Run(() =>
            {
                string fullPath = GetValidatedFullPath(path);
                if (!FileValidationService.IsPathSafe(fullPath))
                {
                    throw new UnauthorizedAccessException("Path is invalid or delete access is denied.");
                }

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                else if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, recursive); // Use the recursive flag
                }
                else
                {
                    throw new FileNotFoundException("Path not found.", path);
                }
            });
        }

        // Updated MovePathAsync
        public async Task MovePathAsync(string sourcePath, string destinationPath)
        {
            await Task.Run(() =>
            {
                string fullSourcePath = GetValidatedFullPath(sourcePath);
                string fullDestinationPath = GetValidatedFullPath(destinationPath);

                if (!FileValidationService.IsPathSafe(fullSourcePath) || !FileValidationService.IsPathSafe(fullDestinationPath))
                {
                    throw new UnauthorizedAccessException("Source or destination path is invalid or access is denied.");
                }
                
                // Ensure destination directory exists for file moves
                if (File.Exists(fullSourcePath)) {
                    var destDir = Path.GetDirectoryName(fullDestinationPath);
                    if(destDir != null && !Directory.Exists(destDir)) {
                        Directory.CreateDirectory(destDir);
                    }
                    File.Move(fullSourcePath, fullDestinationPath);
                }
                else if (Directory.Exists(fullSourcePath))
                {
                     // For directory move, ensure parent of destination exists if destination itself is a new name
                    var destParentDir = Path.GetDirectoryName(fullDestinationPath);
                    if (destParentDir != null && !Directory.Exists(destParentDir) && !Directory.Exists(fullDestinationPath))
                    {
                        Directory.CreateDirectory(destParentDir);
                    }
                    Directory.Move(fullSourcePath, fullDestinationPath);
                }
                else
                {
                    throw new FileNotFoundException("Source path not found.", sourcePath);
                }
            });
        }

        // Updated CopyFileAsync
        public async Task CopyFileAsync(string sourceFilePath, string destinationFilePath, bool overwrite)
        {
            await Task.Run(() =>
            {
                string fullSourcePath = GetValidatedFullPath(sourceFilePath);
                string fullDestinationPath = GetValidatedFullPath(destinationFilePath);

                if (!FileValidationService.IsPathSafe(fullSourcePath) || !FileValidationService.IsPathSafe(fullDestinationPath))
                {
                    throw new UnauthorizedAccessException("Source or destination path is invalid or copy access is denied.");
                }
                if (!File.Exists(fullSourcePath))
                {
                    throw new FileNotFoundException("Source file not found.", sourceFilePath);
                }
                // Ensure destination directory exists
                var destDir = Path.GetDirectoryName(fullDestinationPath);
                if(destDir != null && !Directory.Exists(destDir)) {
                    Directory.CreateDirectory(destDir);
                }
                File.Copy(fullSourcePath, fullDestinationPath, overwrite);
            });
        }

        // Updated CopyDirectoryAsync
        public async Task CopyDirectoryAsync(string sourceDir, string destDir, bool overwriteFiles, bool respectGitignore)
        {
            await Task.Run(() => // Make the lambda async if await is used inside, or use .Result carefully
            {
                string fullSourceDir = GetValidatedFullPath(sourceDir);
                string fullDestDir = GetValidatedFullPath(destDir);

                if (!FileValidationService.IsPathSafe(fullSourceDir) || !FileValidationService.IsPathSafe(fullDestDir))
                {
                    throw new UnauthorizedAccessException("Source or destination path is invalid or copy access is denied.");
                }

                var dir = new System.IO.DirectoryInfo(fullSourceDir);
                if (!dir.Exists)
                {
                    throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");
                }

                Directory.CreateDirectory(fullDestDir);
                List<GitignoreRule> gitignoreRules = respectGitignore ? GitignoreService.LoadGitignoreRules(fullSourceDir) : new List<GitignoreRule>();

                foreach (System.IO.FileInfo file in dir.GetFiles().Where(f => !respectGitignore || !GitignoreService.IsPathIgnored(f.FullName, false, gitignoreRules)))
                {
                    string targetFilePath = Path.Combine(fullDestDir, file.Name);
                    file.CopyTo(targetFilePath, overwriteFiles);
                }

                // Recursive copy
                foreach (System.IO.DirectoryInfo subDir in dir.GetDirectories().Where(d => !respectGitignore || !GitignoreService.IsPathIgnored(d.FullName, true, gitignoreRules)))
                {
                    // For recursive calls, paths should be relative to the original source/dest for user,
                    // but GetValidatedFullPath will make them absolute for internal use.
                    // The CopyDirectoryAsync method itself expects paths that GetValidatedFullPath can handle.
                    string newSourceRelativePath = Path.Combine(sourceDir, subDir.Name); 
                    string newDestRelativePath = Path.Combine(destDir, subDir.Name);     
                    CopyDirectoryAsync(newSourceRelativePath, newDestRelativePath, overwriteFiles, respectGitignore).Wait(); // .Wait() or .Result can be problematic, consider alternatives for deep recursion
                }
            });
        }

        // SearchFilesAsync (renamed from SearchFiles to align with FileTools expectation, and ensure it uses SearchService.SearchFilesAsync)
        public async Task<IEnumerable<FileInfoContract>> SearchFilesAsync(string directoryPath, string pattern, bool respectGitignore, string[]? excludePatterns = null)
        {
            var filesFoundPaths = await SearchService.SearchFilesAsync(directoryPath, pattern, respectGitignore, excludePatterns);            var results = new List<FileInfoContract>();
            foreach (var filePath in filesFoundPaths)
            {
                results.Add(new FileInfoContract { FullName = filePath, Name = Path.GetFileName(filePath), Exists = true }); 
            }
            return results;
        }

        /// <summary>
        /// Attempts to replace text in a line with intelligent line ending fallback.
        /// If the initial match fails, tries different line ending combinations.
        /// </summary>
        /// <param name="line">The line to search and replace in</param>
        /// <param name="oldText">The text to find and replace</param>
        /// <param name="newText">The replacement text</param>
        /// <returns>A tuple indicating success and the updated line</returns>
        private static (bool success, string updatedLine) TryReplaceWithLineEndingFallback(string line, string oldText, string newText)
        {
            // First try direct match (already normalized)
            if (line.Contains(oldText))
            {
                return (true, line.Replace(oldText, newText));
            }

            // Create variants of oldText with different line endings
            var oldTextVariants = new[]
            {
                oldText.Replace("\n", "\r\n"),  // Convert \n to \r\n
                oldText.Replace("\r\n", "\n"),  // Convert \r\n to \n
                oldText.Replace("\r", "\n"),    // Convert \r to \n
                oldText.Replace("\n", "\r"),    // Convert \n to \r
                oldText.Replace("\r\n", "\r")   // Convert \r\n to \r
            };

            // Try each variant
            foreach (var variant in oldTextVariants)
            {
                if (variant != oldText && line.Contains(variant))
                {
                    return (true, line.Replace(variant, newText));
                }
            }

            // No match found with any line ending variant
            return (false, line);
        }
    }
}