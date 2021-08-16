using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

[Serializable]
public abstract class ExposedProperty : ScriptableObject {
    public PropertyValue value = new PropertyValue();
    public abstract object GetValue();
    public abstract void SetValue(PropertyValue value);
    public void ChangePropertyName(string newName){
        name = newName;
        AssetDatabase.SaveAssets();
    }
}