﻿namespace SourceGenerators.MorpehHelpers.NonSemantic {
    using System.Text;

    public static class MorpehDefines {
        public const string MORPEH_DEBUG                = "MORPEH_DEBUG";
        public const string MORPEH_PROFILING            = "MORPEH_PROFILING";
        public const string MORPEH_ENABLE_IL2CPP_CHECKS = "MORPEH_ENABLE_IL2CPP_CHECKS";
        
        public static StringBuilder AppendMorpehDebugDefines(this StringBuilder sb) {
            sb.AppendLine("#if UNITY_EDITOR");
            sb.AppendLine("#define MORPEH_DEBUG");
            sb.AppendLine("#define MORPEH_PROFILING");
            sb.AppendLine("#endif");
            sb.AppendLine("#if !MORPEH_DEBUG");
            sb.AppendLine("#define MORPEH_DEBUG_DISABLED");
            sb.AppendLine("#endif");
            return sb;
        }
        
        public static StringBuilder AppendMorpehUnityDefines(this StringBuilder sb) {
            sb.AppendLine("#if ENABLE_MONO || ENABLE_IL2CPP");
            sb.AppendLine("#define MORPEH_UNITY");
            sb.AppendLine("#endif");
            return sb;
        }
    }
}