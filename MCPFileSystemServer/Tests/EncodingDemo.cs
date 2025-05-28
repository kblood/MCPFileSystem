using MCPFileSystem.Contracts;
using MCPFileSystemServer.Services;
using MCPFileSystemServer.Utilities;
using System.Text;

namespace MCPFileSystemServer.Tests;

/// <summary>
/// Test program to demonstrate the enhanced encoding functionality.
/// This shows how the MCPFileSystem now supports multiple encodings for reading and writing files.
/// </summary>
public class EncodingDemo
{
    public static async Task RunEncodingTests()
    {
        Console.WriteLine("=== MCPFileSystem Encoding Support Demo ===\n");

        // Set up test directory
        var testDir = Path.Combine(Path.GetTempPath(), "MCPFileSystemEncodingTests");
        Directory.CreateDirectory(testDir);
        var fileService = new FileService(testDir);

        try
        {
            await TestBasicEncodingOperations(fileService);
            await TestEncodingDetection(fileService);
            await TestEncodingPreservation(fileService);
            await TestAutoDetectMode(fileService);
            
            Console.WriteLine("\n✅ All encoding tests completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Test failed: {ex.Message}");
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    private static async Task TestBasicEncodingOperations(FileService fileService)
    {
        Console.WriteLine("📝 Testing Basic Encoding Operations");
        Console.WriteLine("====================================");

        var testText = "Hello, 世界! 🌍 Ñoño UTF-8 test with émojis and spéciál characters.";
        
        // Test different encodings
        var encodings = new[]
        {
            FileEncoding.Utf8NoBom,
            FileEncoding.Utf8WithBom,
            FileEncoding.Utf16Le,
            FileEncoding.Ascii // This will lose special characters
        };

        foreach (var encoding in encodings)
        {
            var fileName = $"test_{encoding}.txt";
            var options = new FileWriteOptions { Encoding = encoding };
            
            try
            {
                // Write with specific encoding
                await fileService.WriteFileAsync(fileName, testText, options);
                
                // Read back and verify
                var response = await fileService.ReadFileAsync(fileName);
                var readText = string.Join(Environment.NewLine, response.Lines ?? Array.Empty<string>());
                
                Console.WriteLine($"  {encoding,-15}: ✅ Written and read successfully");
                Console.WriteLine($"                   Detected: {response.Encoding}");
                
                if (encoding == FileEncoding.Ascii)
                {
                    Console.WriteLine($"                   Note: Special characters lost in ASCII encoding");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {encoding,-15}: ❌ Error - {ex.Message}");
            }
        }
        Console.WriteLine();
    }

    private static async Task TestEncodingDetection(FileService fileService)
    {
        Console.WriteLine("🔍 Testing Encoding Detection");
        Console.WriteLine("=============================");

        // Create files with different BOMs manually
        var testFiles = new Dictionary<string, (byte[] bom, string content)>
        {
            ["utf8_with_bom.txt"] = (new byte[] { 0xEF, 0xBB, 0xBF }, "UTF-8 with BOM test"),
            ["utf16le_with_bom.txt"] = (new byte[] { 0xFF, 0xFE }, "UTF-16 LE test"),
            ["utf16be_with_bom.txt"] = (new byte[] { 0xFE, 0xFF }, "UTF-16 BE test"),
            ["no_bom.txt"] = (Array.Empty<byte>(), "Plain ASCII text with no BOM")
        };        foreach (var (fileName, (bom, content)) in testFiles)
        {
            // Write file with specific BOM
            var filePath = Path.Combine(FileValidationService.BaseDirectory, fileName);
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var fullBytes = bom.Concat(contentBytes).ToArray();
            await File.WriteAllBytesAsync(filePath, fullBytes);

            // Test detection
            var detectedEncoding = await EncodingUtility.DetectFileEncodingAsync(filePath);
            var response = await fileService.ReadFileAsync(fileName);
            
            Console.WriteLine($"  {fileName,-20}: Detected as {detectedEncoding}");
            Console.WriteLine($"                       Response encoding: {response.Encoding}");
        }
        Console.WriteLine();
    }

    private static async Task TestEncodingPreservation(FileService fileService)
    {
        Console.WriteLine("🔒 Testing Encoding Preservation");
        Console.WriteLine("=================================");

        // Create a UTF-8 file with BOM
        var originalFile = "preserve_test.txt";
        var originalContent = "Original content with émojis 🎉";
        var options = new FileWriteOptions { Encoding = FileEncoding.Utf8WithBom };
        
        await fileService.WriteFileAsync(originalFile, originalContent, options);
        
        // Read to verify initial encoding
        var initialResponse = await fileService.ReadFileAsync(originalFile);
        Console.WriteLine($"  Initial encoding: {initialResponse.Encoding}");
        
        // Edit with preservation enabled
        var preserveOptions = new FileWriteOptions 
        { 
            Encoding = FileEncoding.Utf8NoBom, // This should be ignored
            PreserveOriginalEncoding = true 
        };
        
        var newContent = originalContent + "\nAdded line with preservation enabled";
        await fileService.WriteFileAsync(originalFile, newContent, preserveOptions);
        
        // Verify encoding was preserved
        var preservedResponse = await fileService.ReadFileAsync(originalFile);
        Console.WriteLine($"  After preservation: {preservedResponse.Encoding}");
        
        if (initialResponse.Encoding == preservedResponse.Encoding)
        {
            Console.WriteLine("  ✅ Encoding successfully preserved!");
        }
        else
        {
            Console.WriteLine("  ❌ Encoding was not preserved");
        }
        Console.WriteLine();
    }

    private static async Task TestAutoDetectMode(FileService fileService)
    {
        Console.WriteLine("🤖 Testing Auto-Detect Mode");
        Console.WriteLine("============================");

        // Create files with different encodings
        var files = new[]
        {
            ("auto_ascii.txt", "Simple ASCII text", FileEncoding.Ascii),
            ("auto_utf8.txt", "UTF-8 with special chars: café", FileEncoding.Utf8WithBom),
            ("auto_utf16.txt", "UTF-16 text", FileEncoding.Utf16Le)
        };

        foreach (var (fileName, content, encoding) in files)
        {
            // Write with specific encoding
            var writeOptions = new FileWriteOptions { Encoding = encoding };
            await fileService.WriteFileAsync(fileName, content, writeOptions);
            
            // Read with auto-detect
            var response = await fileService.ReadFileAsync(fileName, forceEncoding: FileEncoding.AutoDetect);
            Console.WriteLine($"  {fileName,-15}: Detected as {response.Encoding}");
            
            // Read with explicit encoding override
            var overrideResponse = await fileService.ReadFileAsync(fileName, forceEncoding: FileEncoding.Utf8NoBom);
            Console.WriteLine($"                     Override as UTF8: {overrideResponse.Encoding}");
        }
        Console.WriteLine();
    }
}
