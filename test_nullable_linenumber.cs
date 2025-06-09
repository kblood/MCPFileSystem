using System;
using MCPFileSystem.Contracts;

class TestNullableLineNumber
{
    static void Main()
    {
        Console.WriteLine("Testing Nullable LineNumber Functionality");
        Console.WriteLine("=========================================");
        
        // Test 1: Line-based edit (traditional)
        Console.WriteLine("\n1. Testing line-based edit (LineNumber specified):");
        var lineBasedEdit = new FileEdit
        {
            LineNumber = 5,
            Type = EditType.Replace,
            Text = "console.log('This is a line-based replacement');",
            OldText = null
        };
        
        var result1 = lineBasedEdit.Validate();
        Console.WriteLine($"   Valid: {result1.IsValid}");
        if (!result1.IsValid)
        {
            foreach (var error in result1.Errors)
                Console.WriteLine($"   Error: {error}");
        }
        
        // Test 2: Text-based edit (LineNumber is null)
        Console.WriteLine("\n2. Testing text-based edit (LineNumber is null):");
        var textBasedEdit = new FileEdit
        {
            LineNumber = null,
            Type = EditType.Replace,
            Text = "console.log('This is a text-based replacement');",
            OldText = "console.log('Old message');"
        };
        
        var result2 = textBasedEdit.Validate();
        Console.WriteLine($"   Valid: {result2.IsValid}");
        if (!result2.IsValid)
        {
            foreach (var error in result2.Errors)
                Console.WriteLine($"   Error: {error}");
        }
        
        // Test 3: Invalid text-based edit (LineNumber is null but OldText is missing)
        Console.WriteLine("\n3. Testing invalid text-based edit (missing OldText):");
        var invalidTextBasedEdit = new FileEdit
        {
            LineNumber = null,
            Type = EditType.Replace,
            Text = "console.log('This should fail');",
            OldText = null
        };
        
        var result3 = invalidTextBasedEdit.Validate();
        Console.WriteLine($"   Valid: {result3.IsValid}");
        if (!result3.IsValid)
        {
            foreach (var error in result3.Errors)
                Console.WriteLine($"   Error: {error}");
        }
        
        // Test 4: Invalid line-based edit (LineNumber is 0)
        Console.WriteLine("\n4. Testing invalid line-based edit (LineNumber = 0):");
        var invalidLineBasedEdit = new FileEdit
        {
            LineNumber = 0,
            Type = EditType.Replace,
            Text = "console.log('This should fail');",
            OldText = null
        };
        
        var result4 = invalidLineBasedEdit.Validate();
        Console.WriteLine($"   Valid: {result4.IsValid}");
        if (!result4.IsValid)
        {
            foreach (var error in result4.Errors)
                Console.WriteLine($"   Error: {error}");
        }
        
        Console.WriteLine("\nTest completed successfully!");
    }
}
