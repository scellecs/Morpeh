﻿namespace SourceGenerators.Generators.Systems {
    using System.Linq;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using MorpehHelpers.NonSemantic;
    using MorpehHelpers.Semantic;
    using Utils.NonSemantic;
    using Utils.Pools;

    [Generator]
    public class InitializerSourceGenerator : IIncrementalGenerator {
        private const string ATTRIBUTE_NAME = "Initializer";
        
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var classes = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => s is TypeDeclarationSyntax typeDeclaration &&
                                                typeDeclaration.AttributeLists.Any(x => x.Attributes.Any(y => y?.Name.ToString() == ATTRIBUTE_NAME)),
                    transform: static (ctx, _) => ((TypeDeclarationSyntax)ctx.Node, ctx.SemanticModel))
                .Where(static typeDeclaration => typeDeclaration.Item1 is not null);
            
            context.RegisterSourceOutput(classes, static (spc, pair) =>
            {
                var (typeDeclaration, semanticModel) = pair;
                var typeName = typeDeclaration.Identifier.ToString();
                var stashes  = MorpehComponentHelpersSemantic.GetStashRequirements(typeDeclaration, semanticModel);

                var sb     = StringBuilderPool.Get();
                var indent = IndentSourcePool.Get();
                
                sb.AppendMorpehDebugDefines();
                sb.AppendUsings(typeDeclaration, indent).AppendLine();
                sb.AppendBeginNamespace(typeDeclaration, indent).AppendLine();

                sb.AppendIl2CppAttributes(indent);
                sb.AppendIndent(indent)
                    .AppendVisibility(typeDeclaration)
                    .Append(" partial ")
                    .AppendTypeDeclarationType(typeDeclaration)
                    .Append(' ')
                    .Append(typeName)
                    .AppendGenericParams(typeDeclaration)
                    .Append(" : IInitializer ")
                    .AppendGenericConstraints(typeDeclaration)
                    .AppendLine(" {");
                
                
                using (indent.Scope()) {
                    sb.AppendIndent(indent).AppendLine("public World World { get; set; }");
                    
                    sb.AppendLine().AppendLine();
                    foreach (var stash in stashes) {
                        sb.AppendIndent(indent).Append("private ").Append(stash.fieldTypeName).Append(' ').Append(stash.fieldName).AppendLine(";");
                    }
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).Append("public ").Append(typeName).AppendLine("(World world) {");
                    using (indent.Scope()) {
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).Append("MLogger.BeginSample(\"").Append(typeName).AppendLine("_Constructor\");");
                        sb.AppendEndIfDefine();
                        
                        sb.AppendIndent(indent).AppendLine("World = world;");
                        
                        foreach (var stash in stashes) {
                            sb.AppendIndent(indent).Append(stash.fieldName).Append(" = ").Append(stash.metadataClassName).AppendLine(".GetStash(world);");
                        }
                        
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).AppendLine("MLogger.EndSample();");
                        sb.AppendEndIfDefine();
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("public void CallAwake() {");
                    using (indent.Scope()) {
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).Append("MLogger.BeginSample(\"").Append(typeName).AppendLine("_Awake\");");
                        sb.AppendEndIfDefine();
                        
                        sb.AppendIfDefine(MorpehDefines.MORPEH_DEBUG);
                        sb.AppendIndent(indent).AppendLine("try {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).AppendLine("OnAwake();");
                        }
                        sb.AppendIndent(indent).AppendLine("} catch (Exception exception) {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).Append("MLogger.LogError(\"Exception in ").Append(typeName).AppendLine(" initializer (OnAwake)\");");
                            sb.AppendIndent(indent).AppendLine("MLogger.LogException(exception);");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                        sb.AppendElseDefine();
                        sb.AppendIndent(indent).AppendLine("OnAwake();");
                        sb.AppendEndIfDefine();
                        
                        sb.AppendIndent(indent).AppendLine("World.Commit();");
                        
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).AppendLine("MLogger.EndSample();");
                        sb.AppendEndIfDefine();
                    }
                    sb.AppendIndent(indent).AppendLine("}");

                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("public void CallDispose() {");
                    using (indent.Scope()) {
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).Append("MLogger.BeginSample(\"").Append(typeName).AppendLine("_Dispose\");");
                        sb.AppendEndIfDefine();
                        
                        sb.AppendIfDefine(MorpehDefines.MORPEH_DEBUG);
                        sb.AppendIndent(indent).AppendLine("try {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).AppendLine("Dispose();");
                        }
                        sb.AppendIndent(indent).AppendLine("} catch (Exception exception) {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).Append("MLogger.LogError(\"Exception in ").Append(typeName).AppendLine(" initializer (Dispose)\");");
                            sb.AppendIndent(indent).AppendLine("MLogger.LogException(exception);");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                        sb.AppendElseDefine();
                        sb.AppendIndent(indent).AppendLine("Dispose();");
                        sb.AppendEndIfDefine();
                        
                        sb.AppendIndent(indent).AppendLine("World.Commit();");
                        
                        sb.AppendIfDefine(MorpehDefines.MORPEH_PROFILING);
                        sb.AppendIndent(indent).AppendLine("MLogger.EndSample();");
                        sb.AppendEndIfDefine();
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                }
                
                sb.AppendIndent(indent).AppendLine("}");
                sb.AppendEndNamespace(typeDeclaration, indent);
                
                spc.AddSource($"{typeDeclaration.Identifier.Text}.initializer_{typeDeclaration.GetStableFileCompliantHash()}.g.cs", sb.ToString());
                
                StringBuilderPool.Return(sb);
                IndentSourcePool.Return(indent);
            });
        }
    }
}