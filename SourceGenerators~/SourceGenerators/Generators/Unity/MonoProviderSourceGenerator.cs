﻿namespace SourceGenerators.Generators.Unity {
    using System.Linq;
    using Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using MorpehHelpers.NonSemantic;
    using MorpehHelpers.Semantic;
    using Utils.NonSemantic;
    using Utils.Pools;
    using Utils.Semantic;

    [Generator]
    public class MonoProviderSourceGenerator : IIncrementalGenerator {
        public void Initialize(IncrementalGeneratorInitializationContext context) {
            var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
                MorpehAttributes.MONO_PROVIDER_FULL_NAME,
                (s, _) => s is ClassDeclarationSyntax,
                (ctx, _) => (ctx.TargetNode as ClassDeclarationSyntax, ctx.TargetSymbol as INamedTypeSymbol, ctx.Attributes));

            context.RegisterSourceOutput(classes, static (spc, pair) => {
                var (typeDeclaration, typeSymbol, monoProviderAttributes) = pair;
                if (typeDeclaration is null || typeSymbol is null) {
                    return;
                }
                
                if (!RunDiagnostics(spc, typeDeclaration)) {
                    return;
                }
                
                var attribute = monoProviderAttributes.FirstOrDefault();
                if (attribute is null) {
                    return;
                }
                
                INamedTypeSymbol? monoProviderType = null;
                
                if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0] is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol positionalSymbol }) {
                    monoProviderType = positionalSymbol;
                }
                
                if (monoProviderType is null) {
                    return;
                }
                
                var isValidatable               = false;
                var isValidatableWithGameObject = false;
                
                var interfaces = monoProviderType.AllInterfaces;
                for (int i = 0, length = interfaces.Length; i < length; i++) {
                    var interfaceName = interfaces[i].ToDisplayString();
                    switch (interfaceName) {
                        case MorpehAttributes.VALIDATABLE_FULL_NAME:
                            isValidatable = true;
                            break;
                        case MorpehAttributes.VALIDATABLE_WITH_GAMEOBJECT_FULL_NAME:
                            isValidatableWithGameObject = true;
                            break;
                    }
                }
                
                var providerTypeName            = monoProviderType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                var providerStashVariation      = MorpehComponentHelpersSemantic.GetStashVariation(monoProviderType);
                var providerStashSpecialization = MorpehComponentHelpersSemantic.GetStashSpecialization(providerStashVariation, providerTypeName);
                var isTag                       = providerStashVariation == StashVariation.Tag;
                
                var typeName = typeDeclaration.Identifier.ToString();
                
                var sb     = StringBuilderPool.Get();
                var indent = IndentSourcePool.Get();
                
                sb.AppendIndent(indent).AppendLine("using Sirenix.OdinInspector;");
                sb.AppendIndent(indent).AppendLine("using UnityEngine;");
                sb.AppendIndent(indent).AppendLine("using Scellecs.Morpeh;");
                sb.AppendBeginNamespace(typeDeclaration, indent).AppendLine();
                
                sb.AppendIl2CppAttributes(indent);
                sb.AppendIndent(indent)
                    .AppendVisibility(typeDeclaration)
                    .Append(" partial ")
                    .AppendTypeDeclarationType(typeDeclaration)
                    .Append(' ')
                    .Append(typeName)
                    .AppendGenericParams(typeDeclaration)
                    .Append(" : Scellecs.Morpeh.Providers.EntityProvider ")
                    .AppendGenericConstraints(typeDeclaration)
                    .AppendLine(" {");

                using (indent.Scope()) {
                    if (!isTag) {
                        sb.AppendIndent(indent).AppendLine("[SerializeField]");
                        sb.AppendIndent(indent).AppendLine("[HideInInspector]");
                        sb.AppendIndent(indent).Append("private ").Append(providerTypeName).AppendLine(" serializedData;");
                    }
                    
                    sb.AppendIndent(indent).Append("private ").Append(providerStashSpecialization.type).AppendLine(" stash;");
                    
                    if (!isTag) {
                        sb.AppendLine().AppendLine();
                        sb.AppendIfDefine("UNITY_EDITOR");
                        sb.AppendIndent(indent).AppendLine("[PropertySpace]");
                        sb.AppendIndent(indent).AppendLine("[ShowInInspector]");
                        sb.AppendIndent(indent).AppendLine("[PropertyOrder(1)]");
                        sb.AppendIndent(indent).AppendLine("[HideLabel]");
                        sb.AppendIndent(indent).AppendLine("[InlineProperty]");
                        sb.AppendEndIfDefine();
                        sb.AppendIndent(indent).Append("private ").Append(providerTypeName).AppendLine(" Data {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).AppendLine("get {");
                            using (indent.Scope()) {
                                sb.AppendIndent(indent).AppendLine("if (World.Default?.Has(this.cachedEntity) == true) {");
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).Append("var data = this.Stash.Get(this.cachedEntity, out var exist);").AppendLine();
                                    sb.AppendIndent(indent).Append("if (exist) {").AppendLine();
                                    using (indent.Scope()) {
                                        sb.AppendIndent(indent).Append("return data;").AppendLine();
                                    }
                                    sb.AppendIndent(indent).AppendLine("}");
                                }
                                sb.AppendIndent(indent).AppendLine("}");
                                sb.AppendIndent(indent).AppendLine("return this.serializedData;");
                            }
                            sb.AppendIndent(indent).AppendLine("}");
                            
                            sb.AppendIndent(indent).AppendLine("set {");
                            using (indent.Scope()) {
                                sb.AppendIndent(indent).AppendLine("if (World.Default?.Has(this.cachedEntity) == true) {");
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).Append("this.Stash.Set(this.cachedEntity, value);").AppendLine();
                                }
                                sb.AppendIndent(indent).AppendLine("}");
                                sb.AppendIndent(indent).AppendLine("else {");
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).Append("this.serializedData = value;").AppendLine();
                                }
                                sb.AppendIndent(indent).AppendLine("}");
                            }
                            sb.AppendIndent(indent).AppendLine("}");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                    }
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendVisibility(monoProviderType).Append(" ").Append(providerStashSpecialization.type).AppendLine(" Stash {");
                    using (indent.Scope()) {
                        sb.AppendIndent(indent).AppendLine("get {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).AppendLine("if (this.stash == null) {");
                            using (indent.Scope()) {
                                sb.AppendIndent(indent).Append("this.stash = ").Append(providerTypeName).AppendLine(".GetStash(World.Default);");
                            }
                            sb.AppendIndent(indent).AppendLine("}");
                            sb.AppendIndent(indent).AppendLine("return this.stash;");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                    }
                    sb.AppendIndent(indent).AppendLine("}");

                    if (!isTag) {
                        sb.AppendLine().AppendLine();
                        sb.AppendIndent(indent).Append("public ref ").Append(providerTypeName).AppendLine(" GetSerializedData() => ref this.serializedData;");
                        
                        sb.AppendLine().AppendLine();
                        sb.AppendIndent(indent).Append("public ref ").Append(providerTypeName).AppendLine(" GetData() {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).Append("var ent = this.Entity;").AppendLine();
                            sb.AppendIndent(indent).Append("if (World.Default?.Has(this.cachedEntity) == true) {").AppendLine();
                            using (indent.Scope()) {
                                sb.AppendIndent(indent).Append("if (this.Stash.Has(ent)) {").AppendLine();
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).Append("return ref this.Stash.Get(ent);").AppendLine();
                                }
                                sb.AppendIndent(indent).AppendLine("}");
                            }
                            sb.AppendIndent(indent).AppendLine("}");
                            sb.AppendIndent(indent).AppendLine("return ref this.serializedData;");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                        
                        sb.AppendLine().AppendLine();
                        sb.AppendIndent(indent).Append("public ref ").Append(providerTypeName).AppendLine(" GetData(out bool existOnEntity) {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).Append("if (World.Default?.Has(this.cachedEntity) == true) {").AppendLine();
                            using (indent.Scope()) {
                                sb.AppendIndent(indent).Append("return ref this.Stash.Get(this.cachedEntity, out existOnEntity);").AppendLine();
                            }
                            sb.AppendIndent(indent).AppendLine("}");
                            sb.AppendIndent(indent).AppendLine("existOnEntity = false;");
                            sb.AppendIndent(indent).AppendLine("return ref this.serializedData;");
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                        
                        sb.AppendLine().AppendLine();
                        sb.AppendIndent(indent).AppendLine("protected virtual void OnValidate() {");
                        using (indent.Scope()) {
                            if (isValidatable) {
                                sb.AppendIndent(indent).AppendLine("this.serializedData.OnValidate();");
                            }
                        
                            if (isValidatableWithGameObject) {
                                sb.AppendIndent(indent).Append("this.serializedData.OnValidate(this.gameObject);").AppendLine();
                            }
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                    }
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("protected sealed override void PreInitialize() {");
                    using (indent.Scope()) {
                        if (isTag) {
                            sb.AppendIndent(indent).AppendLine("this.Stash.Set(this.cachedEntity);");
                        }
                        else {
                            sb.AppendIndent(indent).AppendLine("this.Stash.Set(this.cachedEntity, this.serializedData);");
                        }
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                    
                    sb.AppendLine().AppendLine();
                    sb.AppendIndent(indent).AppendLine("protected sealed override void PreDeinitialize() {");
                    using (indent.Scope()) {
                        sb.AppendIndent(indent).AppendLine("var ent = this.Entity;");
                        sb.AppendIndent(indent).AppendLine("if (World.Default?.Has(ent) == true) {");
                        using (indent.Scope()) {
                            sb.AppendIndent(indent).Append("this.Stash.Remove(ent);").AppendLine();
                        }
                        sb.AppendIndent(indent).AppendLine("}");
                    }
                    sb.AppendIndent(indent).AppendLine("}");
                }
                
                sb.AppendIndent(indent).AppendLine("}");
                sb.AppendEndNamespace(typeDeclaration, indent);
                
                spc.AddSource($"{typeName}.monoprovider_{typeSymbol.GetFullyQualifiedNameHash()}.g.cs", sb.ToStringAndReturn());
                
                IndentSourcePool.Return(indent);
            });
        }
        
        private static bool RunDiagnostics(SourceProductionContext spc, TypeDeclarationSyntax typeDeclaration) {
            var success = true;

            if (typeDeclaration.IsDeclaredInsideAnotherType()) {
                Errors.ReportNestedDeclaration(spc, typeDeclaration);
                success = false;
            }

            return success;
        }
    }
}