namespace Morpeh.Globals {
    using System;
    using System.Collections.Generic;
    using Unity.IL2CPP.CompilerServices;
    using UnityEngine;
    using Object = UnityEngine.Object;

    [Il2CppSetOption(Option.NullChecks, false)]
    [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
    [Il2CppSetOption(Option.DivideByZeroChecks, false)]
    [CreateAssetMenu(menuName = "Morpeh/Globals/Variables/Variable List Object")]
    public class GlobalVariableListObject : BaseGlobalVariable<List<Object>> {
        public override IDataWrapper Wrapper {
            get => new ListObjectWrapper {list = this.value};
            set => this.Value = ((ListObjectWrapper) value).list;
        }
        
        public override bool CanBeAutoSaved => false;

        protected override List<Object> Load(string serializedData) => JsonUtility.FromJson<ListObjectWrapper>(serializedData).list;

        protected override string Save() => JsonUtility.ToJson(new ListObjectWrapper{list = this.value});

        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [Il2CppSetOption(Option.DivideByZeroChecks, false)]
        [Serializable]
        private class ListObjectWrapper : IDataWrapper {
            public List<Object> list;
        }
    }
}