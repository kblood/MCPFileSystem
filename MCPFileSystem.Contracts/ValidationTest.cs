using System;
using System.Collections.Generic;
using MCPFileSystem.Contracts;

namespace MCPFileSystem.Contracts.Test
{
    class ValidationTest
    {
        static void Main()
        {
            Console.WriteLine("Testing FileEdit Validation (Simple Text Replacement)");
            Console.WriteLine("======================================");
            
            // Test valid edit
            var validEdit = new FileEdit
            {
                OldText = "foo",
                Text = "bar"
            };
            validEdit.NormalizeText();
            var result = validEdit.Validate();
            Console.WriteLine(result.IsValid ? "✅ Edit validation passed" : $"❌ Edit validation failed: {string.Join(", ", result.Errors)}");
            
            // Test missing OldText
            var missingOldText = new FileEdit { Text = "bar" };
            var result2 = missingOldText.Validate();
            Console.WriteLine(result2.IsValid ? "❌ Should have failed (missing OldText)" : "✅ Properly failed for missing OldText");
            
            // Test missing Text
            var missingText = new FileEdit { OldText = "foo" };
            var result3 = missingText.Validate();
            Console.WriteLine(result3.IsValid ? "❌ Should have failed (missing Text)" : "✅ Properly failed for missing Text");
            
            Console.WriteLine("\nValidation test completed!");
        }
    }
}
