using System.Text;
using System.IO;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MCPFileSystemServer.Services;

/// <summary>
/// Provides code analysis operations for C# source files.
/// </summary>
public static class CodeAnalysisService
{

/// <summary>
    /// Lists all .csproj files in the base directory.
    /// </summary>
    /// <returns>An array of .csproj file paths.</returns>
    public static string[] ListProjects()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            return Directory.GetFiles(baseDir, "*.csproj", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all .csproj files in the specified directory.
    /// </summary>
    /// <param name="directory">The directory to search in.</param>
    /// <returns>An array of .csproj file paths.</returns>
    public static string[] ListProjectsInDirectory(string directory)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directory);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {directory}" };
            }

            return Directory.GetFiles(normalizedPath, "*.csproj", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all .sln files in the base directory.
    /// </summary>
    /// <returns>An array of .sln file paths.</returns>
    public static string[] ListSolutions()
    {
        try
        {
            var baseDir = FileValidationService.BaseDirectory;
            return Directory.GetFiles(baseDir, "*.sln", SearchOption.AllDirectories);
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Lists all source files in a project directory.
    /// </summary>
    /// <param name="projectDir">The project directory to search in.</param>
    /// <returns>An array of source file paths.</returns>
    public static string[] ListSourceFiles(string projectDir)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(projectDir);

            if (!Directory.Exists(normalizedPath))
            {
                return new[] { $"Error: Directory not found: {projectDir}" };
            }

            // Common source file extensions
            var extensions = new[] { "*.cs", "*.vb", "*.fs", "*.ts", "*.js", "*.html", "*.css", "*.sql", "*.json", "*.xml" };
            var files = new List<string>();

            foreach (var extension in extensions)
            {
                files.AddRange(Directory.GetFiles(normalizedPath, extension, SearchOption.AllDirectories));
            }

            // Filter out common binary folders
            var result = files.Where(f => !f.Contains("\\bin\\") && 
                                         !f.Contains("\\obj\\") && 
                                         !f.Contains("\\node_modules\\")).ToArray();

            return result;
        }
        catch (Exception ex)
        {
            return new[] { $"Error: {ex.Message}" };
        }
    }

    /// <summary>
    /// Extracts an outline of classes, methods, and properties from C# code files using Roslyn.
    /// </summary>
    /// <param name="filePath">Path to the C# file to analyze.</param>
    /// <returns>A hierarchical outline of the code structure.</returns>
    public static Dictionary<string, object> GetCodeOutline(string filePath)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(filePath);

            if (!File.Exists(normalizedPath))
            {
                return new Dictionary<string, object> 
                { 
                    ["error"] = $"File not found: {filePath}" 
                };
            }

            // Check if this is a C# file
            if (!normalizedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                return new Dictionary<string, object> 
                { 
                    ["error"] = "Only C# files are supported for code outline" 
                };
            }

            var content = File.ReadAllText(normalizedPath);
            
            // Simple outline result
            var result = new Dictionary<string, object>
            {
                ["file"] = Path.GetFileName(normalizedPath),
                ["path"] = normalizedPath,
                ["namespaces"] = new List<Dictionary<string, object>>()
            };

            // Parse the source code using Roslyn
            var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetCompilationUnitRoot();

            // Extract using directives
            var usings = root.Usings
                .Select(u => u.Name?.ToString() ?? string.Empty)
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();

            if (usings.Count > 0)
            {
                result["usings"] = usings;
            }

            // Extract all namespaces
            var namespaces = new Dictionary<string, Dictionary<string, object>>();

            // Process all namespace declarations (including file-scoped namespaces)
            foreach (var namespaceDecl in root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.NamespaceDeclarationSyntax>())
            {
                var namespaceName = namespaceDecl.Name.ToString();
                
                if (!namespaces.ContainsKey(namespaceName))
                {
                    var namespaceDict = new Dictionary<string, object>
                    {
                        ["name"] = namespaceName,
                        ["classes"] = new List<Dictionary<string, object>>()
                    };
                    namespaces[namespaceName] = namespaceDict;
                    ((List<Dictionary<string, object>>)result["namespaces"]).Add(namespaceDict);
                }

                // Process types in this namespace
                ProcessTypesInContainer(namespaceDecl, namespaces[namespaceName]);
            }

            // Process file-scoped namespace (if any)
            var fileScopedNamespace = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FileScopedNamespaceDeclarationSyntax>().FirstOrDefault();
            if (fileScopedNamespace != null)
            {
                var namespaceName = fileScopedNamespace.Name.ToString();
                
                if (!namespaces.ContainsKey(namespaceName))
                {
                    var namespaceDict = new Dictionary<string, object>
                    {
                        ["name"] = namespaceName,
                        ["classes"] = new List<Dictionary<string, object>>()
                    };
                    namespaces[namespaceName] = namespaceDict;
                    ((List<Dictionary<string, object>>)result["namespaces"]).Add(namespaceDict);
                }
                
                // Process types in this namespace
                ProcessTypesInContainer(fileScopedNamespace, namespaces[namespaceName]);
            }

            // Process types that aren't in a namespace (global)
            var globalTypes = new List<Dictionary<string, object>>();
            foreach (var type in root.DescendantNodes()
                .Where(n => n.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.CompilationUnitSyntax)
                .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
            {
                globalTypes.Add(ExtractTypeInfo(type));
            }

            if (globalTypes.Count > 0)
            {
                result["global_types"] = globalTypes;
            }

            return result;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    /// <summary>
    /// Generate code outline for all files in a directory using Roslyn.
    /// </summary>
    /// <param name="directoryPath">Path to the directory to analyze.</param>
    /// <param name="filePattern">File pattern to filter (default: *.cs).</param>
    /// <param name="recursive">Whether to search subdirectories.</param>
    /// <returns>A collection of code outlines.</returns>
    public static Dictionary<string, object> GetCodeOutlinesForDirectory(string directoryPath, string filePattern = "*.cs", bool recursive = true)
    {
        try
        {
            var normalizedPath = FileValidationService.NormalizePath(directoryPath);

            if (!Directory.Exists(normalizedPath))
            {
                return new Dictionary<string, object> { ["error"] = $"Directory not found: {directoryPath}" };
            }

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var files = Directory.GetFiles(normalizedPath, filePattern, searchOption);

            // Skip bin and obj folders
            files = files.Where(f => !f.Contains("\\bin\\") && !f.Contains("\\obj\\")).ToArray();

            var result = new Dictionary<string, object>
            {
                ["directory"] = normalizedPath,
                ["fileCount"] = files.Length,
                ["outlines"] = new List<Dictionary<string, object>>()
            };

            // Generate a summary of types and members found
            var typeSummary = new Dictionary<string, int>
            {
                ["classes"] = 0,
                ["interfaces"] = 0,
                ["structs"] = 0,
                ["enums"] = 0,
                ["records"] = 0,
                ["methods"] = 0,
                ["properties"] = 0,
                ["fields"] = 0
            };

            // Process a maximum of 50 files to avoid excessive output
            foreach (var file in files.Take(50))
            {
                var outline = GetCodeOutline(file);
                if (!outline.ContainsKey("error"))
                {
                    // Add to result
                    ((List<Dictionary<string, object>>)result["outlines"]).Add(outline);
                    
                    // Update summary statistics
                    UpdateTypeSummary(outline, typeSummary);
                }
            }

            // Add summary to the result
            result["summary"] = typeSummary;

            if (files.Length > 50)
            {
                result["note"] = $"Output limited to 50 files. {files.Length - 50} files were omitted.";
            }

            return result;
        }
        catch (Exception ex)
        {
            return new Dictionary<string, object> { ["error"] = ex.Message };
        }
    }

    /// <summary>
    /// Updates the type summary statistics based on a code outline.
    /// </summary>
    private static void UpdateTypeSummary(Dictionary<string, object> outline, Dictionary<string, int> summary)
    {
        // Process namespaces
        if (outline.TryGetValue("namespaces", out var namespacesObj) && namespacesObj is List<Dictionary<string, object>> namespaces)
        {
            foreach (var ns in namespaces)
            {
                // Process classes in each namespace
                if (ns.TryGetValue("classes", out var classesObj) && classesObj is List<Dictionary<string, object>> classes)
                {
                    foreach (var cls in classes)
                    {
                        // Update type count
                        string typeKind = cls.TryGetValue("type", out var typeObj) ? typeObj?.ToString() ?? "class" : "class";
                        if (!string.IsNullOrEmpty(typeKind))
                        {
                            switch (typeKind.ToLowerInvariant())
                            {
                                case "class": summary["classes"]++; break;
                                case "interface": summary["interfaces"]++; break;
                                case "struct": summary["structs"]++; break;
                                case "enum": summary["enums"]++; break;
                                case "record": summary["records"]++; break;
                            }
                        }

                        // Update member counts
                        if (cls.TryGetValue("members", out var membersObj) && membersObj is List<Dictionary<string, object>> members)
                        {
                            foreach (var member in members)
                            {
                                string memberKind = member.TryGetValue("kind", out var kindObj) ? kindObj?.ToString() ?? "" : "";
                                if (!string.IsNullOrEmpty(memberKind))
                                {
                                    switch (memberKind.ToLowerInvariant())
                                    {
                                        case "method": summary["methods"]++; break;
                                        case "property": summary["properties"]++; break;
                                        case "field": summary["fields"]++; break;
                                        // Nested types and other members are counted separately
                                    }
                                }

                                // If this is a nested type, recursively count its members
                                if (memberKind == "nested_type" && member.TryGetValue("members", out var nestedMembersObj) && 
                                    nestedMembersObj is List<Dictionary<string, object>> nestedMembers)
                                {
                                    // Update nested type count
                                    string nestedTypeKind = member.TryGetValue("type", out var nestedTypeObj) ? nestedTypeObj?.ToString() ?? "class" : "class";
                                    if (!string.IsNullOrEmpty(nestedTypeKind))
                                    {
                                        switch (nestedTypeKind.ToLowerInvariant())
                                        {
                                            case "class": summary["classes"]++; break;
                                            case "interface": summary["interfaces"]++; break;
                                            case "struct": summary["structs"]++; break;
                                            case "enum": summary["enums"]++; break;
                                            case "record": summary["records"]++; break;
                                        }
                                    }

                                    // Count nested type's members
                                    foreach (var nestedMember in nestedMembers)
                                    {
                                        string nestedMemberKind = nestedMember.TryGetValue("kind", out var nestedKindObj) ? nestedKindObj?.ToString() ?? "" : "";
                                        if (!string.IsNullOrEmpty(nestedMemberKind))
                                        {
                                            switch (nestedMemberKind.ToLowerInvariant())
                                            {
                                                case "method": summary["methods"]++; break;
                                                case "property": summary["properties"]++; break;
                                                case "field": summary["fields"]++; break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // Process global types
        if (outline.TryGetValue("global_types", out var globalTypesObj) && globalTypesObj is List<Dictionary<string, object>> globalTypes)
        {
            foreach (var type in globalTypes)
            {
                // Update type count
                string typeKind = type.TryGetValue("type", out var typeObj) ? typeObj?.ToString() ?? "class" : "class";
                if (!string.IsNullOrEmpty(typeKind))
                {
                    switch (typeKind.ToLowerInvariant())
                    {
                        case "class": summary["classes"]++; break;
                        case "interface": summary["interfaces"]++; break;
                        case "struct": summary["structs"]++; break;
                        case "enum": summary["enums"]++; break;
                        case "record": summary["records"]++; break;
                    }
                }

                // Update member counts
                if (type.TryGetValue("members", out var membersObj) && membersObj is List<Dictionary<string, object>> members)
                {
                    foreach (var member in members)
                    {
                        string memberKind = member.TryGetValue("kind", out var kindObj) ? kindObj?.ToString() ?? "" : "";
                        if (!string.IsNullOrEmpty(memberKind))
                        {
                            switch (memberKind.ToLowerInvariant())
                            {
                                case "method": summary["methods"]++; break;
                                case "property": summary["properties"]++; break;
                                case "field": summary["fields"]++; break;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Processes all types within a namespace or other container.
    /// </summary>
    private static void ProcessTypesInContainer(Microsoft.CodeAnalysis.SyntaxNode container, Dictionary<string, object> containerDict)
    {
        foreach (var type in container.DescendantNodes()
            .Where(n => n.Parent == container || (n.Parent is Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax && n.Parent.Parent == container))
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
        {
            var typeDict = ExtractTypeInfo(type);
            ((List<Dictionary<string, object>>)containerDict["classes"]).Add(typeDict);
        }
    }

    /// <summary>
    /// Extracts information about a C# type (class, interface, struct, enum, record).
    /// </summary>
    private static Dictionary<string, object> ExtractTypeInfo(Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax type)
    {
        var typeDict = new Dictionary<string, object>
        {
            ["name"] = type.Identifier.Text,
            ["type"] = GetTypeKind(type),
            ["access"] = GetAccessibility(type),
            ["modifiers"] = GetModifiers(type),
            ["members"] = new List<Dictionary<string, object>>()
        };

        // Add inheritance/interface info if present
        if (type.BaseList != null && type.BaseList.Types.Count > 0)
        {
            typeDict["inherits"] = string.Join(", ", type.BaseList.Types.Select(t => t.ToString()));
        }

        // Process methods
        foreach (var method in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>())
        {
            // Skip if this method is not a direct child of the current type (it could be in a nested type)
            if (method.Parent != type) continue;

            var methodDict = new Dictionary<string, object>
            {
                ["kind"] = "method",
                ["name"] = method.Identifier.Text,
                ["returnType"] = method.ReturnType.ToString(),
                ["access"] = GetAccessibility(method),
                ["modifiers"] = GetModifiers(method),
                ["parameters"] = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))
            };

            ((List<Dictionary<string, object>>)typeDict["members"]).Add(methodDict);
        }

        // Process properties
        foreach (var property in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>())
        {
            // Skip if not direct child
            if (property.Parent != type) continue;

            var propertyDict = new Dictionary<string, object>
            {
                ["kind"] = "property",
                ["name"] = property.Identifier.Text,
                ["type"] = property.Type.ToString(),
                ["access"] = GetAccessibility(property),
                ["modifiers"] = GetModifiers(property)
            };

            ((List<Dictionary<string, object>>)typeDict["members"]).Add(propertyDict);
        }

        // Process fields
        foreach (var field in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>())
        {
            // Skip if not direct child
            if (field.Parent != type) continue;

            foreach (var variable in field.Declaration.Variables)
            {
                var fieldDict = new Dictionary<string, object>
                {
                    ["kind"] = "field",
                    ["name"] = variable.Identifier.Text,
                    ["type"] = field.Declaration.Type.ToString(),
                    ["access"] = GetAccessibility(field),
                    ["modifiers"] = GetModifiers(field)
                };

                ((List<Dictionary<string, object>>)typeDict["members"]).Add(fieldDict);
            }
        }

        // Process events
        foreach (var evt in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.EventFieldDeclarationSyntax>())
        {
            // Skip if not direct child
            if (evt.Parent != type) continue;

            foreach (var variable in evt.Declaration.Variables)
            {
                var eventDict = new Dictionary<string, object>
                {
                    ["kind"] = "event",
                    ["name"] = variable.Identifier.Text,
                    ["type"] = evt.Declaration.Type.ToString(),
                    ["access"] = GetAccessibility(evt),
                    ["modifiers"] = GetModifiers(evt)
                };

                ((List<Dictionary<string, object>>)typeDict["members"]).Add(eventDict);
            }
        }

        // Process constructors
        foreach (var ctor in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ConstructorDeclarationSyntax>())
        {
            // Skip if not direct child
            if (ctor.Parent != type) continue;

            var ctorDict = new Dictionary<string, object>
            {
                ["kind"] = "constructor",
                ["name"] = ctor.Identifier.Text,
                ["access"] = GetAccessibility(ctor),
                ["modifiers"] = GetModifiers(ctor),
                ["parameters"] = string.Join(", ", ctor.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"))
            };

            ((List<Dictionary<string, object>>)typeDict["members"]).Add(ctorDict);
        }

        // Process nested types
        foreach (var nestedType in type.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>())
        {
            // Only process direct children
            if (nestedType.Parent != type) continue;

            // Skip self-reference
            if (nestedType == type) continue;

            var nestedTypeDict = ExtractTypeInfo(nestedType);
            nestedTypeDict["kind"] = "nested_type";
            ((List<Dictionary<string, object>>)typeDict["members"]).Add(nestedTypeDict);
        }

        return typeDict;
    }

    /// <summary>
    /// Gets the kind of a C# type declaration (class, interface, struct, record, enum).
    /// </summary>
    private static string GetTypeKind(TypeDeclarationSyntax type)
    {
        if (type is ClassDeclarationSyntax) return "class";
        if (type is InterfaceDeclarationSyntax) return "interface";
        if (type is StructDeclarationSyntax) return "struct";
        if (type is RecordDeclarationSyntax) return "record";
        return "unknown"; // EnumDeclarationSyntax is not a TypeDeclarationSyntax
    }

    /// <summary>
    /// Gets the accessibility (public, private, etc.) of a member declaration.
    /// </summary>
    private static string GetAccessibility(Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member)
    {
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PublicKeyword)))
            return "public";
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PrivateKeyword)))
            return "private";
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ProtectedKeyword)))
            return "protected";
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.InternalKeyword)))
            return "internal";
        
        // Default accessibility
        if (member is Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax)
            return "internal";
        else
            return "private";
    }

    /// <summary>
    /// Gets the modifiers (static, abstract, etc.) of a member declaration.
    /// </summary>
    private static string GetModifiers(Microsoft.CodeAnalysis.CSharp.Syntax.MemberDeclarationSyntax member)
    {
        var modifiers = new List<string>();
        
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.StaticKeyword)))
            modifiers.Add("static");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AbstractKeyword)))
            modifiers.Add("abstract");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.SealedKeyword)))
            modifiers.Add("sealed");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VirtualKeyword)))
            modifiers.Add("virtual");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.OverrideKeyword)))
            modifiers.Add("override");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.AsyncKeyword)))
            modifiers.Add("async");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ReadOnlyKeyword)))
            modifiers.Add("readonly");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.VolatileKeyword)))
            modifiers.Add("volatile");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.ExternKeyword)))
            modifiers.Add("extern");
        if (member.Modifiers.Any(m => m.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.PartialKeyword)))
            modifiers.Add("partial");

        return string.Join(" ", modifiers);
    }
}
