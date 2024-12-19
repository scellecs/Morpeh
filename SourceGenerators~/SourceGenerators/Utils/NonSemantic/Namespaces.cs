﻿namespace SourceGenerators.Utils.NonSemantic {
    using System.Text;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Namespaces {
        public static BaseNamespaceDeclarationSyntax? GetNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) {
            SyntaxNode? current = typeDeclarationSyntax;

            while (current != null) {
                if (current is BaseNamespaceDeclarationSyntax baseNamespaceDeclaration) {
                    return baseNamespaceDeclaration;
                }
                current = current.Parent;
            }

            return null;
        }
        
        public static bool IsGlobalNamespace(this TypeDeclarationSyntax typeDeclarationSyntax) => typeDeclarationSyntax.Parent is CompilationUnitSyntax;

        public static StringBuilder AppendNamespaceName(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax) {
            var ns = GetNamespace(typeDeclarationSyntax);
            
            if (ns != null) {
                sb.Append(ns.Name);
            }
            
            return sb;
        }
        
        public static StringBuilder AppendBeginNamespace(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, int indent = 0) {
            var ns = GetNamespace(typeDeclarationSyntax);
            
            if (ns != null) {
                sb.AppendIndent(indent).Append("namespace ").Append(ns.Name).Append(" {");
            }
            
            return sb;
        }
        
        public static StringBuilder AppendBeginNamespace(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, IndentSource indent) {
            var ns = GetNamespace(typeDeclarationSyntax);
            
            if (ns != null) {
                sb.AppendIndent(indent).Append("namespace ").Append(ns.Name).Append(" {");
                indent.Right();
            }
            
            return sb;
        }
        
        public static StringBuilder AppendEndNamespace(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, int indent = 0) {
            var ns = GetNamespace(typeDeclarationSyntax);
            
            if (ns != null) {
                sb.AppendIndent(indent).Append("}");
            }
            
            return sb;
        }
        
        public static StringBuilder AppendEndNamespace(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, IndentSource indent) {
            var ns = GetNamespace(typeDeclarationSyntax);
            
            if (ns != null) {
                indent.Left();
                sb.AppendIndent(indent).Append("}");
            }
            
            return sb;
        }

        public static StringBuilder AppendUsings(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, int indent = 0) {
            SyntaxNode? current = typeDeclarationSyntax;

            while (current != null) {
                switch (current) {
                    case BaseNamespaceDeclarationSyntax baseNamespaceDeclaration: {
                        foreach (var usingDirective in baseNamespaceDeclaration.Usings) {
                            sb.AppendIndent(indent).AppendLine(usingDirective.ToString());
                        }

                        break;
                    }
                    case CompilationUnitSyntax compilationUnitSyntax: {
                        foreach (var usingDirective in compilationUnitSyntax.Usings) {
                            sb.AppendIndent(indent).AppendLine(usingDirective.ToString());
                        }

                        break;
                    }
                }

                current = current.Parent;
            }

            return sb;
        }
        
        public static StringBuilder AppendUsings(this StringBuilder sb, TypeDeclarationSyntax typeDeclarationSyntax, IndentSource indent) {
            SyntaxNode? current = typeDeclarationSyntax;

            while (current != null) {
                switch (current) {
                    case BaseNamespaceDeclarationSyntax baseNamespaceDeclaration: {
                        foreach (var usingDirective in baseNamespaceDeclaration.Usings) {
                            sb.AppendIndent(indent).AppendLine(usingDirective.ToString());
                        }

                        break;
                    }
                    case CompilationUnitSyntax compilationUnitSyntax: {
                        foreach (var usingDirective in compilationUnitSyntax.Usings) {
                            sb.AppendIndent(indent).AppendLine(usingDirective.ToString());
                        }

                        break;
                    }
                }

                current = current.Parent;
            }

            return sb;
        }
    }
}