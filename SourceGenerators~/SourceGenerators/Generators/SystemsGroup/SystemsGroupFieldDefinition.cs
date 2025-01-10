﻿namespace SourceGenerators.Generators.SystemsGroup {
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using MorpehHelpers.NonSemantic;

    public class SystemsGroupFieldDefinition {
        public IFieldSymbol?           fieldSymbol;
        public MorpehLoopTypeSemantic.LoopDefinition? loopType;
        public bool                    isSystem;
        public bool                    isInitializer;
        public bool                    isDisposable;
        public bool                    isInjectable;
        public bool                    register;
        public INamedTypeSymbol?       registerAs;
        
        public void Reset() {
            this.fieldSymbol      = null;
            this.loopType         = null;
            this.isSystem         = false;
            this.isInitializer    = false;
            this.isDisposable     = false;
            this.isInjectable     = false;
            this.register         = false;
            this.registerAs       = null;
        }
    }
}