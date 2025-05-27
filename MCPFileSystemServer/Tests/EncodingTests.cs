using MCPFileSystem.Contracts;
using MCPFileSystemServer.Services;
using MCPFileSystemServer.Utilities;
using System.Text;

namespace MCPFileSystemServer.Tests;

/// <summary>
/// Test and demonstration of encoding functionality.
/// </summary>
public static class EncodingTests
{
    /// <summary>
    /// Demonstrates various encoding operations.
    /// </summary>
    public static async Task RunEncodingTests(string basePath)
    {
        var fileService = new FileService(basePath);
        var testDir = Path.Combine(basePath, "encoding_tests");
        
        if (!Directory.Exists(testDir))
        {
            Directory.CreateDirectory(testDir);
        }

        Console.WriteLine("=== MCPFileSystem Encoding Tests ===\n");

        // Test 1: Write files with different encodings
        await TestWriteWithDifferentEncodings(fileService, testDir);
        
        // Test 2: Test encoding detection
        await TestEncodingDetection(fileService, testDir);
        
        // Test 3: Test encoding preservation during edits
        await TestEncodingPreservation(fileService, testDir);
        
        // Test 4: Test auto-detection mode
        await TestAutoDetection(fileService, testDir);

        Console.WriteLine("\n=== All Encoding Tests Completed ===");
    }

    private static async Task TestWriteWithDifferentEncodings(FileService fileService, string testDir)
    {
        Console.WriteLine("1. Testing writing files with different encodings:");
        
        var testContent = "Hello, World! 🌍\nLine 2: Special chars: äöü\nLine 3: Numbers: 123";
        
        var encodings = new[]
        {
            FileEncoding.Utf8NoBom,
            FileEncoding.Utf8WithBom,
            FileEncoding.Utf16Le,
            FileEncoding.Ascii
        };

        foreach (var encoding in encodings)
        {
            var fileName = $"test_{encoding.ToString().ToLower()}.txt";
            var filePath = Path.Combine(testDir, fileName);
            
            var options = new FileWriteOptions { Encoding = encoding };
            
            try
            {
                await fileService.WriteFileAsync(filePath, testContent, options);
                
                // Read back to verify
                var readResponse = await fileService.ReadFileAsync(filePath);
                Console.WriteLine($"   ✓ {encoding}: Written and read back successfully. Detected: {readResponse.Encoding}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ✗ {encoding}: Failed - {ex.Message}");
            }
        }
        Console.WriteLine();
    }

    private static async Task TestEncodingDetection(FileService fileService, string testDir)
    {
        Console.WriteLine("2. Testing encoding detection:");
        
        // Create files with different encodings manually
        var testFiles = new Dictionary<string, (Encoding encoding, string expectedDetection)>
        {
            ["manual_utf8_bom.txt"] = (new UTF8Encoding(true), "Utf8WithBom"),
            ["manual_utf8_nobom.txt"] = (new UTF8Encoding(false), "Utf8NoBom"),
            ["manual_utf16le.txt"] = (Encoding.Unicode, "Utf16Le"),
            ["manual_ascii.txt"] = (Encoding.ASCII, "Ascii")
        };

        var content = "Test content for encoding detection";
        
        foreach (var kvp in testFiles)
        {
            var filePath = Path.Combine(testDir, kvp.Key);
            await File.WriteAllTextAsync(filePath, content, kvp.Value.encoding);
            
            // Test detection
            var detectedEncoding = await EncodingUtility.DetectFileEncodingAsync(filePath);
            var readResponse = await fileService.ReadFileAsync(filePath);
            
            Console.WriteLine($"   File: {kvp.Key}");
            Console.WriteLine($"     Expected: {kvp.Value.expectedDetection}");
            Console.WriteLine($"     Detected: {detectedEncoding}");
            Console.WriteLine($"     Read as: {readResponse.Encoding}");
            Console.WriteLine();
        }
    }

    private static async Task TestEncodingPreservation(FileService fileService, string testDir)
    {
        Console.WriteLine("3. Testing encoding preservation during edits:");
        
        // Create a UTF-8 with BOM file
        var filePath = Path.Combine(testDir, "preserve_test.txt");
        var options = new FileWriteOptions { Encoding = FileEncoding.Utf8WithBom };
        var originalContent = "Original line 1\nOriginal line 2\nOriginal line 3";
        
        await fileService.WriteFileAsync(filePath, originalContent, options);
        
        // Read to confirm encoding
        var beforeEdit = await fileService.ReadFileAsync(filePath);
        Console.WriteLine($"   Before edit - Encoding: {beforeEdit.Encoding}");
        
        // Perform edit with encoding preservation
        var edits = new List<FileEdit>
        {
            new FileEdit { LineNumber = 2, Type = EditType.Replace, Text = "Modified line 2" }
        };
        
        var editResult = await fileService.EditFileAsync(filePath, edits, dryRun: false, preserveEncoding: true);
        Console.WriteLine($"   Edit result - Success: {editResult.Success}, Preserved: {editResult.PreservedEncoding}");
        
        // Read after edit to confirm encoding preservation
        var afterEdit = await fileService.ReadFileAsync(filePath);
        Console.WriteLine($"   After edit - Encoding: {afterEdit.Encoding}");
        Console.WriteLine($"   Content preserved: {beforeEdit.Encoding == afterEdit.Encoding}");
        Console.WriteLine();
    }

    private static async Task TestAutoDetection(FileService fileService, string testDir)
    {
        Console.WriteLine("4. Testing auto-detection mode:");
        
        var filePath = Path.Combine(testDir, "auto_detect_test.txt");
        var content = "Content for auto-detection test\nSecond line with ümlaut";
        
        // Write with auto-detect (should default to UTF-8 no BOM)
        var options = new FileWriteOptions { Encoding = FileEncoding.AutoDetect };
        await fileService.WriteFileAsync(filePath, content, options);
        
        // Read with auto-detection
        var readResponse = await fileService.ReadFileAsync(filePath, forceEncoding: FileEncoding.AutoDetect);
        Console.WriteLine($"   Written with AutoDetect, read as: {readResponse.Encoding}");
        Console.WriteLine($"   Content lines: {readResponse.Lines.Length}");
        Console.WriteLine();
    }
}
