using System;
using System.Collections.Generic;
using System.Linq;
using MCPFileSystem.Contracts;

namespace MCPFileSystem.Contracts.Test
{
    class ValidationTest
    {
        static void Main()
        {
            Console.WriteLine("Testing FileEdit Validation Improvements");
            Console.WriteLine("=======================================");
            
            // Test the specific scenario that would have caused the original AI error
            TestMultiLineJavaScriptEdit();
            
            Console.WriteLine("\nValidation test completed!");
        }
        
        static void TestMultiLineJavaScriptEdit()
        {
            Console.WriteLine("\nTesting multi-line JavaScript edit (original problem scenario):");
            
            // This simulates the type of edit that the AI was trying to make
            var complexEdit = new FileEdit
            {
                LineNumber = 1,
                Type = EditType.Replace,
                Text = "function updateAnimation() {\n    // Enhanced animation with proper error handling\n    const duration = 1000;\n    const easing = 'ease-in-out';\n    \n    try {\n        requestAnimationFrame(() => {\n            console.log(`Animating with duration: ${duration}ms, easing: ${easing}`);\n            // Animation logic here\n        });\n    } catch (error) {\n        console.error('Animation failed:', error);\n    }\n}",
                OldText = "function updateAnimation() {\n    // Old implementation\n    console.log('updating...');\n}"
            };
            
            // Test normalization
            complexEdit.NormalizeText();
            Console.WriteLine($"✅ Text normalization completed");
            
            // Test validation
            var result = complexEdit.Validate();
            
            if (result.IsValid)
            {
                Console.WriteLine($"✅ Edit validation passed");
                Console.WriteLine($"   Text length: {complexEdit.Text?.Length} characters");
                Console.WriteLine($"   Contains proper newlines: {(complexEdit.Text?.Contains("\\n") == true ? "Yes" : "No")}");
            }
            else
            {
                Console.WriteLine($"❌ Edit validation failed:");
                foreach (var error in result.Errors)
                {
                    Console.WriteLine($"   - {error}");
                }
            }
            
            // Test JSON helper validation
            Console.WriteLine("\nTesting JSON helper validation:");
            
            var validJson = "[{\"LineNumber\":1,\"Type\":\"Replace\",\"Text\":\"function test() {\\n    console.log('test');\\n}\"}]";
            var (edits, errors) = FileEditJsonHelper.DeserializeEdits(validJson);
            
            if (errors.Any())
            {
                Console.WriteLine($"❌ JSON validation errors: {string.Join("; ", errors)}");
            }
            else
            {
                Console.WriteLine($"✅ JSON validation passed. {edits?.Count} edits parsed.");
            }
            
            // Test invalid JSON (literal newlines)
            var invalidJson = "[{\"LineNumber\":1,\"Type\":\"Replace\",\"Text\":\"function test() {\n    console.log('test');\n}\"}]";
            var (invalidEdits, invalidErrors) = FileEditJsonHelper.DeserializeEdits(invalidJson);
            
            if (invalidErrors.Any())
            {
                Console.WriteLine($"✅ Invalid JSON properly caught: {invalidErrors.Count} errors detected");
                Console.WriteLine($"   First error: {invalidErrors.First()}");
            }
            else
            {
                Console.WriteLine($"❌ Invalid JSON should have been caught but wasn't!");
            }
        }
    }
}
