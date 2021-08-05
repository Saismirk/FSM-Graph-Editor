using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[Serializable]
public class ParameterValue {
    public enum ParameterType {Float, Int, Bool}
    public ParameterType type;
    public float FloatValue; 
    public bool BoolValue;
    public int IntValue;
    public void SetFloat(float value) => FloatValue = value;
    public void SetInt(int value) => IntValue = value;
    public void SetBool(bool value) => BoolValue = value;
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ParameterValue))]
public class ParameterValueDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var typeProp = property.FindPropertyRelative("type").enumValueIndex;
        switch ((ParameterValue.ParameterType)typeProp) {
            case ParameterValue.ParameterType.Float:
                EditorGUI.PropertyField(position, property.FindPropertyRelative("FloatValue"), GUIContent.none);
                break;
            case ParameterValue.ParameterType.Int:
                EditorGUI.PropertyField(position, property.FindPropertyRelative("IntValue"), GUIContent.none);
                break;
            case ParameterValue.ParameterType.Bool:
                EditorGUI.PropertyField(position, property.FindPropertyRelative("BoolValue"), GUIContent.none);
                break;
            default:
                break;
        }
    }
}
#endif
