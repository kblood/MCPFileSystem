using System;
using System.Collections.Generic;
using MCPFileSystem.Contracts; // Added to use the correct DirectoryTreeNode

namespace MCPFileSystem.Example
{
    public static class Helpers
    {
        // Helper method to print directory tree
        public static void PrintDirectoryTree(MCPFileSystem.Contracts.DirectoryTreeNode node, int level) // Changed to use Contracts.DirectoryTreeNode
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
