﻿namespace SourceGenerators.Diagnostics {
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    public static class Errors {
        // MORPEH_ERR_001
        
        private static readonly DiagnosticDescriptor NESTED_DECLARATION = new(
            "MORPEH_ERR_001",
            "Component {0} is declared inside another type.",
            "Component {0} is declared inside another type.",
            "Morpeh",
            DiagnosticSeverity.Error,
            true,
            "Component {0} is declared inside another type.");
        
        public static void ReportNestedDeclaration(SourceProductionContext ctx, TypeDeclarationSyntax typeDeclaration) {
            ctx.ReportDiagnostic(Diagnostic.Create(NESTED_DECLARATION, typeDeclaration.GetLocation(), typeDeclaration.Identifier.Text));
        }
        
        // MORPEH_ERR_002
        
        private static readonly DiagnosticDescriptor MISSING_LOOP_TYPE = new(
            "MORPEH_ERR_002",
            "System does not have a [Loop] attribute.",
            "System does not have a [Loop] attribute.",
            "Morpeh",
            DiagnosticSeverity.Error,
            true,
            "System does not have a [Loop] attribute.");
        
        public static void ReportMissingLoopType(SourceProductionContext ctx, FieldDeclarationSyntax fieldDeclaration) {
            ctx.ReportDiagnostic(Diagnostic.Create(MISSING_LOOP_TYPE, fieldDeclaration.GetLocation()));
        }
        
        // MORPEH_ERR_003
        
        private static readonly DiagnosticDescriptor INVALID_INJECTION_TYPE = new(
            "MORPEH_ERR_003",
            "Cannot inject {0} as {1} because it cannot be casted to the type.",
            "Cannot inject {0} as {1} because it cannot be casted to the type.",
            "Morpeh",
            DiagnosticSeverity.Error,
            true,
            "Cannot inject {0} as {1} because it cannot be casted to the type.");
        
        public static void ReportInvalidInjectionType(SourceProductionContext ctx, FieldDeclarationSyntax fieldDeclaration, string typeName, string injectionType) {
            ctx.ReportDiagnostic(Diagnostic.Create(INVALID_INJECTION_TYPE, fieldDeclaration.GetLocation(), typeName, injectionType));
        }
    }
}