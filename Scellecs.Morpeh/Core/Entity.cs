using System.Runtime.CompilerServices;

namespace Scellecs.Morpeh {
    using System;
    using Collections;
    using Sirenix.OdinInspector;
    using Unity.IL2CPP.CompilerServices;
    using UnityEngine;

#if !MORPEH_NON_SERIALIZED
    [Serializable]
#endif
    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    public sealed partial class Entity {
        [NonSerialized]
        internal World world;

        [SerializeField]
        internal EntityId entityId;
        
        [SerializeField]
        internal ArchetypeId currentArchetype;
        
        [ShowInInspector]
        public EntityId ID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.entityId;
        }

        internal Entity() { }

        public override string ToString() => $"Entity:{ID.ToString()}";
    }
}
