using MCPFileSystem.Contracts;
using System;

namespace Test
{
    class Program
    {
        static void Main()
        {
            var options = new FileWriteOptions
            {
                Encoding = FileEncoding.Utf8NoBom,
                PreserveOriginalEncoding = false
            };
            
            Console.WriteLine($"Options created: {options.Encoding}");
        }
    }
}
