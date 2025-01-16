﻿namespace SourceGenerators.Generators.Components {
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Microsoft.CodeAnalysis;
    using MorpehHelpers.NonSemantic;
    using MorpehHelpers.Semantic;
    using Utils.Logging;
    using Utils.NonSemantic;
    using Utils.Pools;

    public static class ComponentSourceGenerator {
        public static void Generate(SourceProductionContext spc, in ComponentToGenerate component) {
            try {
                var source = Generate(component);
                spc.AddSource($"{component.TypeName}.component_{Guid.NewGuid():N}.g.cs", source);
                
                Logger.Log(nameof(ComponentSourceGenerator), nameof(Generate), $"Generated component: {component.TypeName}");
            } catch (Exception e) {
                Logger.LogException(nameof(ComponentSourceGenerator), nameof(Generate), e);
            }
        }
        
        public static string Generate(in ComponentToGenerate component) {
            var fullTypeName = StringBuilderPool.Get().Append(component.TypeName).Append(component.GenericParams).ToStringAndReturn();

            var sb     = StringBuilderPool.Get();
            var indent = IndentSourcePool.Get();

            if (component.TypeNamespace != null) {
                sb.AppendIndent(indent).Append("namespace ").Append(component.TypeNamespace).AppendLine(" {");
                indent.Right();
            }

            sb.AppendIl2CppAttributes(indent);
            sb.AppendIndent(indent)
                .Append(Types.AsString(component.Visibility))
                .Append(" partial struct ")
                .Append(component.TypeName)
                .Append(component.GenericParams)
                .Append(" : ")
                .Append(MorpehComponentHelpersSemantic.GetStashSpecializationConstraintInterface(component.StashVariation))
                .Append(' ')
                .Append(component.GenericConstraints)
                .AppendLine(" {");

            using (indent.Scope()) {
                sb.AppendInlining(MethodImplOptions.AggressiveInlining, indent);
                sb.AppendIndent(indent).Append("public static ")
                    .Append(MorpehComponentHelpersSemantic.GetStashSpecializationType(component.StashVariation, fullTypeName))
                    .Append(" GetStash(Scellecs.Morpeh.World world) => Scellecs.Morpeh.WorldStashExtensions.")
                    .Append(MorpehComponentHelpersSemantic.GetStashSpecializationGetStashMethod(component.StashVariation, fullTypeName))
                    .Append("(world, capacity: ")
                    .Append(component.InitialCapacity)
                    .AppendLine(");");
            }

            sb.AppendIndent(indent).AppendLine("}");

            if (component.TypeNamespace != null) {
                indent.Left();
                sb.AppendIndent(indent).AppendLine("}");
            }

            IndentSourcePool.Return(indent);

            return sb.ToStringAndReturn();
        }
    }
}