using System;
using System.Collections.Generic;

namespace MCPFileSystem.Example
{
    // Model classes for the new features
    
    public class DirectoryTreeNode
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public long? Size { get; set; }
        public List<DirectoryTreeNode> Children { get; set; }
    }
    
    public class DirectoryInfo
    {
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Path { get; set; }
    }
    
    public class SearchResult
    {
        public string Path { get; set; }
        public string Type { get; set; }
    }
    
    public class EditOperation
    {
        public string OldText { get; set; }
        public string NewText { get; set; }
    }
    
    public class EditResult
    {
        public int EditCount { get; set; }
        public string Diff { get; set; }
    }
}
