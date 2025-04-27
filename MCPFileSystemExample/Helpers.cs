using System;
using System.Collections.Generic;

namespace MCPFileSystem.Example
{
    public static class Helpers
    {
        // Helper method to print directory tree
        public static void PrintDirectoryTree(DirectoryTreeNode node, int level)
        {
            string indent = new string(' ', level * 4);
            string prefix = node.Type == "directory" ? "[DIR] " : "[FILE] ";
            
            Console.WriteLine($"{indent}{prefix}{node.Name}");
            
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    PrintDirectoryTree(child, level + 1);
                }
            }
        }
    }
}
