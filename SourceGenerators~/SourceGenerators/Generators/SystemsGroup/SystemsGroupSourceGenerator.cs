﻿namespace SourceGenerators.Generators.SystemsGroup {
    using System.Collections.Immutable;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Diagnostics;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using MorpehHelpers.NonSemantic;
    using Utils.NonSemantic;
    using Utils.Pools;
    using Utils.Semantic;
    
    // TODO: Add string-output Generate method after making inputs DTOs
    public static class SystemsGroupSourceGenerator {
        public static void Generate(SourceProductionContext spc, (TypeDeclarationSyntax?, INamedTypeSymbol?, ImmutableArray<AttributeData>) pair) {
            var (typeDeclaration, typeSymbol, systemsGroupAttributes) = pair;
            if (typeDeclaration is null || typeSymbol is null) {
                return;
            }

            if (!RunDiagnostics(spc, typeDeclaration)) {
                return;
            }

            using var scopedFieldDefinitionCollection = SystemsGroupFieldDefinitionCache.GetScoped();

            var members = typeSymbol.GetMembers();
            for (int i = 0, length = members.Length; i < length; i++) {
                if (members[i] is not IFieldSymbol fieldSymbol) {
                    continue;
                }

                var fieldAttributes = fieldSymbol.GetAttributes();
                var typeAttributes  = fieldSymbol.Type.GetAttributes();

                var fieldDefinition = scopedFieldDefinitionCollection.Create();

                fieldDefinition.fieldSymbol   = fieldSymbol;
                fieldDefinition.loopType      = MorpehLoopTypeSemantic.FindLoopType(fieldAttributes);
                fieldDefinition.isSystem      = typeAttributes.Any(static x => x.AttributeClass?.Name == MorpehAttributes.SYSTEM_NAME);
                fieldDefinition.isInitializer = typeAttributes.Any(static x => x.AttributeClass?.Name == MorpehAttributes.INITIALIZER_NAME);
                fieldDefinition.isDisposable = fieldDefinition is { isSystem: false, isInitializer: false } &&
                                               fieldSymbol.Type.AllInterfaces.Any(x => x.Name == KnownTypes.DISPOSABLE_NAME && x.ToDisplayString() == KnownTypes.DISPOSABLE_FULL_NAME);

#if MORPEH_SOURCEGEN_INJECTABLE_SCAN_SLOW
                fieldDefinition.isInjectable = IsInjectableSlowPath(fieldSymbol.Type);
#else
                fieldDefinition.isInjectable = typeAttributes.Any(static x => x.AttributeClass?.Name == MorpehAttributes.INJECTABLE_NAME);
#endif

                for (int j = 0, jlength = fieldAttributes.Length; j < jlength; j++) {
                    var attribute = fieldAttributes[j];

                    if (attribute.AttributeClass?.Name != MorpehAttributes.REGISTER_NAME) {
                        continue;
                    }

                    fieldDefinition.register   = true;
                    fieldDefinition.registerAs = fieldSymbol.Type as INamedTypeSymbol;

                    if (attribute.ConstructorArguments.Length > 0 && attribute.ConstructorArguments[0] is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol injectAsPositionalSymbol }) {
                        fieldDefinition.registerAs = injectAsPositionalSymbol;
                    }
                    else if (attribute.NamedArguments.Length > 0 && attribute.NamedArguments[0].Value is { Kind: TypedConstantKind.Type, Value: INamedTypeSymbol injectAsNamedSymbol }) {
                        fieldDefinition.registerAs = injectAsNamedSymbol;
                    }

                    break;
                }

                scopedFieldDefinitionCollection.Add(fieldDefinition);
            }

            if (!RunDiagnostics(spc, scopedFieldDefinitionCollection)) {
                return;
            }

            var typeName = typeDeclaration.Identifier.ToString();

            var inlineUpdateMethods = false;
            for (int i = 0, length = systemsGroupAttributes.Length; i < length; i++) {
                var attribute = systemsGroupAttributes[i];

                var args = attribute.ConstructorArguments;
                if (args.Length >= 1 && args[0].Value is bool inlineUpdateMethodsValue) {
                    inlineUpdateMethods = inlineUpdateMethodsValue;
                }
            }

            var sb     = StringBuilderPool.Get();
            var indent = IndentSourcePool.Get();

            sb.AppendMorpehDebugDefines();
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
                .AppendGenericConstraints(typeSymbol)
                .AppendLine(" {");

            using (indent.Scope()) {
                sb.AppendLine().AppendLine();
                sb.AppendIndent(indent).Append("public ").Append(typeName).AppendLine("(Scellecs.Morpeh.World world, Scellecs.Morpeh.InjectionTable injectionTable = null) {");
                using (indent.Scope()) {
                    for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                        var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                        if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                            sb.AppendIndent(indent)
                                .AppendLine($"{fieldDefinition.fieldSymbol?.Name} = new {fieldDefinition.fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}(world);");
                        }
                        else {
                            sb.AppendIndent(indent)
                                .AppendLine($"{fieldDefinition.fieldSymbol?.Name} = new {fieldDefinition.fieldSymbol.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}();");
                        }
                    }

                    // TODO: Count and generate null check for injectionTable if there are any registered fields
                    sb.AppendIndent(indent).AppendLine("if (injectionTable != null) {");
                    using (indent.Scope()) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                            if (fieldDefinition.register) {
                                if (SymbolEqualityComparer.Default.Equals(fieldDefinition.fieldSymbol?.Type, fieldDefinition.registerAs)) {
                                    sb.AppendIndent(indent).Append("injectionTable.Register(").Append(fieldDefinition.fieldSymbol?.Name).AppendLine(");");
                                }
                                else {
                                    sb.AppendIndent(indent).Append("injectionTable.Register(").Append(fieldDefinition.fieldSymbol?.Name).Append(", typeof(")
                                        .Append(fieldDefinition.registerAs.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)).AppendLine("));");
                                }
                            }
                        }
                    }

                    sb.AppendIndent(indent).AppendLine("}");
                }

                sb.AppendIndent(indent).AppendLine("}");

                sb.AppendLine().AppendLine();
                sb.AppendIndent(indent).AppendLine("public void Inject(Scellecs.Morpeh.InjectionTable injectionTable) {");
                using (indent.Scope()) {
                    for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                        var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                        if (fieldDefinition.isInjectable) {
                            sb.AppendIndent(indent).Append(fieldDefinition.fieldSymbol?.Name).AppendLine(".Inject(injectionTable);");
                        }
                    }
                }

                sb.AppendIndent(indent).AppendLine("}");

                sb.AppendLine().AppendLine();
                sb.AppendIndent(indent).AppendLine("public void CallAwake() {");
                using (indent.Scope()) {
                    using (MorpehSyntax.ScopedProfile(sb, typeName, "CallAwake", indent)) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                            if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.CallAwake();");
                            }
                        }
                    }
                }

                sb.AppendIndent(indent).AppendLine("}");

                sb.AppendLine().AppendLine();
                sb.AppendIndent(indent).AppendLine("public void CallDispose(Scellecs.Morpeh.InjectionTable injectionTable = null) {");
                using (indent.Scope()) {
                    using (MorpehSyntax.ScopedProfile(sb, typeName, "CallDispose", indent)) {
                        for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                            var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                            if (fieldDefinition.isSystem || fieldDefinition.isInitializer) {
                                sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.CallDispose();");
                            }
                            else if (fieldDefinition.isDisposable) {
                                sb.AppendIfDefine(MorpehDefines.MORPEH_DEBUG);
                                sb.AppendIndent(indent).AppendLine("try {");
                                sb.AppendEndIfDefine();
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).AppendLine($"{fieldDefinition.fieldSymbol?.Name}.Dispose();");
                                }

                                sb.AppendIfDefine(MorpehDefines.MORPEH_DEBUG);
                                sb.AppendIndent(indent).AppendLine("} catch (global::System.Exception exception) {");
                                using (indent.Scope()) {
                                    sb.AppendIndent(indent).Append("Scellecs.Morpeh.MLogger.LogError(\"Exception in ").Append(typeName).AppendLine(" (Dispose)\");");
                                    sb.AppendIndent(indent).AppendLine("Scellecs.Morpeh.MLogger.LogException(exception);");
                                }

                                sb.AppendIndent(indent).AppendLine("}");
                                sb.AppendEndIfDefine();
                            }
                        }

                        sb.AppendIndent(indent).AppendLine("if (injectionTable != null) {");
                        using (indent.Scope()) {
                            for (int i = 0, length = scopedFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                                var fieldDefinition = scopedFieldDefinitionCollection.Collection.ordered[i];

                                if (fieldDefinition.register) {
                                    sb.AppendIndent(indent).Append("injectionTable.UnRegister(typeof(").Append(fieldDefinition.registerAs.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                                        .AppendLine("));");
                                }
                            }
                        }

                        sb.AppendIndent(indent).AppendLine("}");
                    }
                }

                sb.AppendIndent(indent).AppendLine("}");

                foreach (var loopPair in scopedFieldDefinitionCollection.Collection.byLoopType) {
                    var methodName  = loopPair.Key;
                    var loopMethods = loopPair.Value;

                    sb.AppendLine().AppendLine();
                    if (inlineUpdateMethods) {
                        sb.AppendInlining(MethodImplOptions.AggressiveInlining, indent);
                    }

                    sb.AppendIndent(indent).Append("public void ").Append(methodName).AppendLine("(float deltaTime) {");
                    using (indent.Scope()) {
                        using (MorpehSyntax.ScopedProfile(sb, typeName, methodName, indent)) {
                            foreach (var fieldDefinition in loopMethods) {
                                sb.AppendIndent(indent).Append(fieldDefinition.fieldSymbol?.Name).AppendLine(".CallUpdate(deltaTime);");
                            }
                        }
                    }

                    sb.AppendIndent(indent).AppendLine("}");
                }
            }

            sb.AppendIndent(indent).AppendLine("}");
            sb.AppendEndNamespace(typeDeclaration, indent);

            spc.AddSource($"{typeName}.systemsgroup_{typeSymbol.GetFullyQualifiedNameHash()}.g.cs", sb.ToStringAndReturn());

            IndentSourcePool.Return(indent);
        }
        
        private static bool RunDiagnostics(SourceProductionContext spc, TypeDeclarationSyntax typeDeclaration) {
            var success = true;

            if (typeDeclaration.IsDeclaredInsideAnotherType()) {
                Errors.ReportNestedDeclaration(spc, typeDeclaration);
                success = false;
            }

            return success;
        }

        private static bool RunDiagnostics(SourceProductionContext ctx, ScopedSystemsGroupFieldDefinitionCollection scopedSystemsGroupFieldDefinitionCollection) {
            var success = true;
            
            for (int i = 0, length = scopedSystemsGroupFieldDefinitionCollection.Collection.ordered.Count; i < length; i++) {
                var fieldDefinition = scopedSystemsGroupFieldDefinitionCollection.Collection.ordered[i];

                if (fieldDefinition is { isSystem : true, loopType: null }) {
                    Errors.ReportMissingLoopType(ctx, fieldDefinition.fieldSymbol);
                    success = false;
                }

                if (fieldDefinition is { isSystem: false, loopType: not null }) {
                    Errors.ReportLoopTypeOnNonSystemField(ctx, fieldDefinition.fieldSymbol);
                    success = false;
                }
                
                if (fieldDefinition.register) {
                    if (fieldDefinition.fieldSymbol.Type.TypeKind is not TypeKind.Class) {
                        Errors.ReportInvalidInjectionSourceType(ctx, fieldDefinition.fieldSymbol, fieldDefinition.fieldSymbol.Type.Name);
                        success = false;
                    }
                }

                if (!fieldDefinition.isSystem && !fieldDefinition.isInitializer) {
                    if (fieldDefinition.fieldSymbol.Type is INamedTypeSymbol namedTypeSymbol) {
                        if (!namedTypeSymbol.InstanceConstructors.Any(x => x.Parameters.Length == 0)) {
                            Errors.ReportNoParameterlessConstructor(ctx, fieldDefinition.fieldSymbol, fieldDefinition.fieldSymbol.Type.Name);
                            success = false;
                        }
                    }
                }
            }
            
            return success;
        }
        
#if MORPEH_SOURCEGEN_INJECTABLE_SCAN_SLOW
        private static bool IsInjectableSlowPath(ITypeSymbol typeSymbol) {
            var currentSymbol  = typeSymbol;

            while (currentSymbol is { TypeKind: TypeKind.Class }) {
                var members = currentSymbol.GetMembers();
                
                for (int i = 0, length = members.Length; i < length; i++) {
                    if (members[i] is not IFieldSymbol fieldSymbol || fieldSymbol.IsStatic) {
                        continue;
                    }

                    var attributes = fieldSymbol.GetAttributes();
                    
                    for (int j = 0, attributesLength = attributes.Length; j < attributesLength; j++) {
                        if (attributes[j].AttributeClass?.Name == MorpehAttributes.INJECTABLE_NAME) {
                            return true;
                        }
                    }
                }

                currentSymbol  = currentSymbol.BaseType;
            }

            return false;
        }
#endif
    }
}