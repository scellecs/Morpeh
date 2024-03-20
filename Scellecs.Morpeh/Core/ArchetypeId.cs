﻿namespace Scellecs.Morpeh {
    using System;
    using System.Runtime.CompilerServices;
    using Unity.IL2CPP.CompilerServices;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    [Serializable]
    public readonly struct ArchetypeId : IEquatable<ArchetypeId> {
        private readonly long value;
        
        public static ArchetypeId Invalid => new ArchetypeId(0);
        
        public ArchetypeId(long value) {
            this.value = value;
        }
        
        public ArchetypeId(TypeId typeId) {
            this.value = typeId.GetValue();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long GetValue() {
            return this.value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeId Combine(ArchetypeId otherArchetype) {
            return new ArchetypeId(this.value ^ otherArchetype.value);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ArchetypeId Combine(TypeId typeId) {
            return new ArchetypeId(this.value ^ typeId.GetValue());
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ArchetypeId other) {
            return this.value == other.value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj) {
            return obj is ArchetypeId other && this.Equals(other);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ArchetypeId a, ArchetypeId b) {
            return a.value == b.value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ArchetypeId a, ArchetypeId b) {
            return a.value != b.value;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() {
            return this.value.GetHashCode();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override string ToString() {
            return $"ArchetypeId({this.value})";
        }
    }
}