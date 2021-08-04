using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
[Serializable]
public class ParameterWrapper {
    public string parameterName;
    public Parameter parameter;
    public ParameterWrapper Clone() {
        var param = new ParameterWrapper();
        param.parameterName = parameterName;
        param.parameter = ScriptableObject.Instantiate(parameter);
        param.parameter.name = parameter.name;
        return param;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ParameterWrapper))]
public class ParameterWrapperDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var parameterProp = property.FindPropertyRelative("parameter");
        var parameterNameProp = property.FindPropertyRelative("parameterName");
        var parameterRect = new Rect(position.x, position.y, 150, EditorGUIUtility.singleLineHeight);
        var valueRect = new Rect(position.x + 160, position.y, 100, EditorGUIUtility.singleLineHeight);
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(parameterRect, parameterNameProp, GUIContent.none);
        if (EditorGUI.EndChangeCheck()) {
            (parameterProp.objectReferenceValue as Parameter)?.ChangeParameterName(parameterNameProp.stringValue);
        }
        if (parameterProp != null && parameterProp.objectReferenceValue != null) {
            var parameterSO = new SerializedObject(parameterProp.objectReferenceValue as Parameter);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(valueRect, parameterSO.FindProperty("value"), GUIContent.none);
            if (EditorGUI.EndChangeCheck()) {
                parameterSO.ApplyModifiedProperties();
            }
        }
    }
}
#endif
