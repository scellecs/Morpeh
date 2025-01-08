﻿#if UNITY_EDITOR
using Scellecs.Morpeh.WorldBrowser.Editor.ComponentViewer;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Scellecs.Morpeh.WorldBrowser.Editor {
    internal sealed class InspectorViewModel : BaseViewModel, IInspectorViewModel {
        private readonly InspectorModel model;
        private readonly ComponentStorageModel storage;
        private readonly IInspectorProcessor processor;

        private readonly HashSet<int> expandedStates;
        private readonly List<int> componentTypeIds;
        private readonly Dictionary<int, ComponentDataBoxed> componentData;

        private readonly Func<int, object> Get;
        private readonly Action<int, object> Set;

        private Entity selectedEntity;
        private long modelVersion;

        internal InspectorViewModel(IInspectorProcessor processor, IComponentStorageProcessor storageProcessor) {
            this.model = processor.GetModel();
            this.storage = storageProcessor.GetModel();
            this.processor = processor;
            this.expandedStates = new HashSet<int>();
            this.componentTypeIds = new List<int>();
            this.componentData = new Dictionary<int, ComponentDataBoxed>();
            this.Get = this.GetComponent;
            this.Set = this.SetComponent;
            this.version = 0u;
            this.modelVersion = -1u;
            this.selectedEntity = default;
        }

        public void Update() {
            if (this.modelVersion != this.model.version) {
                this.IncrementVersion();
                this.modelVersion = this.model.version;

                if (this.model.selectedEntity.Equals(default)) {
                    this.expandedStates.Clear();
                    this.componentTypeIds.Clear();
                    this.componentData.Clear();
                    this.selectedEntity = this.model.selectedEntity;
                    return;
                }

                if (!this.selectedEntity.Equals(this.model.selectedEntity)) {
                    this.expandedStates.Clear();
                    this.selectedEntity = this.model.selectedEntity;
                }

                this.componentTypeIds.Clear();
                for (int i = 0; i < this.model.components.Count; i++) {
                    this.componentTypeIds.Add(this.model.components[i].typeId);
                }
            }

            this.componentData.Clear();
            for (int i = 0; i < this.model.components.Count; i++) {
                var component = this.model.components[i];
                this.componentData.Add(component.typeId, component);
            }
        }

        public void SetExpanded(int typeId, bool value) {
            if (value) {
                this.expandedStates.Add(typeId);
            }
            else {
                this.expandedStates.Remove(typeId);
            }
        }

        public void SetExpandedAll(bool value) {
            this.expandedStates.Clear();

            if (value) {
                foreach (var typeId in this.componentTypeIds) {
                    this.expandedStates.Add(typeId);
                }
            }

            this.IncrementVersion();
        }

        public bool IsExpanded(int typeId) {
            return this.expandedStates.Contains(typeId);
        }

        public bool IsNotExpandedAll() {
            return this.expandedStates.Count < this.componentTypeIds.Count;
        }

        public IList GetItemsSource() {
            return this.componentTypeIds;
        }

        public ComponentData GetComponentData(int typeId) {
            var component = this.componentData[typeId];
            var internalId = this.storage.typeIdToInternalId[typeId];
            var name = this.storage.componentNames[internalId];

            return new ComponentData(){
                typeId = component.typeId,
                name = name,
                isMarker = component.isMarker,
                Get = this.Get,
                Set = this.Set,
            };
        }

        private object GetComponent(int typeId) {
            return this.componentData[typeId].data;
        }

        private void SetComponent(int typeId, object value) {
            this.processor.SetComponentData(typeId, value);
        }
    }
}
#endif