using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    [Serializable]
    public abstract class ExposedProperty : ScriptableObject {
        public PropertyValue value = new PropertyValue();
        public abstract object GetValue();
        public virtual void SetValue(PropertyValue value) {
            this.value = value;
        }
        public void ChangePropertyName(string newName){
            name = newName;
            AssetDatabase.SaveAssets();
        }
    }
}