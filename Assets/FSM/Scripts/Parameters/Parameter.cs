using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
[Serializable]
public abstract class Parameter : ScriptableObject {
    public ParameterValue value = new ParameterValue();
    public abstract object GetValue();
    public abstract void SetValue(ParameterValue value);
    public void ChangeParameterName(string newName){
        name = newName;
        AssetDatabase.SaveAssets();
    }
}
