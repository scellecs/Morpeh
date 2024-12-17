﻿namespace SourceGenerators.Generators.SystemsGroup {
    using System.Linq;
    using Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using MorpehHelpers.NonSemantic;
    using Utils.NonSemantic;
    using Utils.Pools;
    using Utils.Semantic;

    [Generator]
    public class SystemsGroupSourceGenerator : IIncrementalGenerator {
        private const string ATTRIBUTE_NAME = "SystemsGroup";
        private const string DISPOSABLE_INTERFACE_NAME = "System.IDisposable";
        
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var classes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is TypeDeclarationSyntax typeDeclaration &&
                                                typeDeclaration.AttributeLists.Any(x => x.Attributes.Any(y => y?.Name.ToString() == ATTRIBUTE_NAME)),
                    transform: static (ctx, _) => (declaration: (TypeDeclarationSyntax)ctx.Node, model: ctx.SemanticModel))
                .Where(static pair => pair.declaration is not null);
            
            var disposableInterface = context.CompilationProvider
                .Select(static (compilation, _) => compilation.GetTypeByMetadataName(DISPOSABLE_INTERFACE_NAME));
            
            context.RegisterSourceOutput(classes.Combine(disposableInterface), static (spc, pair) =>
            {
                var ((typeDeclaration, semanticModel), disposableSymbol) = pair;
                
                if (disposableSymbol is null) {
                    return;
                }
                
                var fields = typeDeclaration.Members
                    .OfType<FieldDeclarationSyntax>()
                    .ToArray();
                
                using var scopedFieldDefinitionCollection = FieldDefinitionCache.GetScoped();
                
                for (int i = 0, length = fields.Length; i < length; i++) {
                    var fieldDeclaration = fields[i];
                    
                    var fieldSymbol = semanticModel.GetDeclaredSymbol(fieldDeclaration.Declaration.Variables.First()) as IFieldSymbol;
                    if (fieldSymbol is null) {
                        continue;
                    }
                    
                    var typeAttributes  = fieldSymbol.Type.GetAttributes();
                    
                    var fieldDefinition = scopedFieldDefinitionCollection.Emplace();
                    
                    fieldDefinition.fieldDeclaration = fieldDeclaration;
                    fieldDefinition.fieldSymbol      = fieldSymbol;
                    fieldDefinition.loopType         = LoopTypeHelpers.GetLoopMethodNameFromField(fieldSymbol);
                    fieldDefinition.isSystem         = typeAttributes.Any(x => x.AttributeClass?.Name == "SystemAttribute");
                    fieldDefinition.isInitializer    = typeAttributes.Any(x => x.AttributeClass?.Name == "InitializerAttribute");
                    fieldDefinition.isDisposable     = fieldSymbol.Type.AllInterfaces.Contains(disposableSymbol);
                    fieldDefinition.isInjectable     = TypesSemantic.ContainsFieldsWithAttribute(fieldSymbol.Type, "InjectableAttribute");
                }
                
                if (!RunDiagnostics(spc, scopedFieldDefinitionCollection)) {
                    return;
                }

                var typeName = typeDeclaration.Identifier.ToString();

                var sb     = StringBuilderPool.Get();
                var indent = IndentSourcePool.Get();
                
                sb.AppendUsings(typeDeclaration).AppendLine();
                sb.AppendBeginNamespace(typeDeclaration, indent).AppendLine();
                
                // TODO: Could be something than public and class? Maybe we should throw a diagnostic if it's otherwise
                sb.AppendIl2CppAttributes(indent);
                sb.AppendIndent(indent).Append("public partial class ").Append(typeName).AppendLine(" {");

                using (indent.Scope()) {
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).Append("public ").Append(typeName).AppendLine("(World world) {");
                    using (indent.Scope()) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                            if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name} = new {fieldDefinition.fieldDeclaration?.Declaration.Type}(world);");
                            } else {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name} = new {fieldDefinition.fieldDeclaration?.Declaration.Type}();");
                            }
                        }
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    // TODO: Implement Inject generation in a separate sourcegen
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("public void Inject(Scellecs.Morpeh.InjectionTable injectionTable) {");
                    using (indent.Scope()) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];
                            
                            if (fieldDefinition.isInjectable) {
                                sb.AppendIndent(indent).AppendLine($"injectionTable.Inject({fieldDefinition.fieldSymbol?.Name});");
                            }
                        }
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("public void CallAwake() {");
                    using (indent.Scope()) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];
                            
                            if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.CallAwake();");
                            }
                        }
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("public void CallDispose() {");
                    using (indent.Scope()) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];
                            
                            if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.CallDispose();");
                            } else if (fieldDefinition.isDisposable) {
                                // TODO: Should we wrap it into a try-catch block for MORPEH_DEBUG?
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.Dispose();");
                            }
                        }
                        
                        // TODO: Remove registrations if they were registered in constructor
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    // TODO: Update loops
                }
                
                sb.AppendIndent(indent).AppendLine("}");
                sb.AppendEndNamespace(typeDeclaration, indent);
                
                spc.AddSource($"{typeDeclaration.Identifier.Text}.systemsgroup_{typeDeclaration.GetStableFileCompliantHash()}.g.cs", sb.ToString());
                
                StringBuilderPool.Return(sb);
                IndentSourcePool.Return(indent);
            });
        }

        private static bool RunDiagnostics(SourceProductionContext ctx, ScopedFieldDefinitionCollection scopedFieldDefinitionCollection) {
            var success = true;
            
            for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                if (fieldDefinition.isSystem && fieldDefinition.loopType is null) {
                    if (fieldDefinition.fieldDeclaration != null) {
                        Errors.ReportMissingLoopType(ctx, fieldDefinition.fieldDeclaration);
                    }

                    success = false;
                }
            }
            
            return success;
        }
    }
}