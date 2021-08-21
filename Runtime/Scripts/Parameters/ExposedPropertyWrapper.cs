using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    [Serializable]
    public class ExposedPropertyWrapper {
        public ExposedProperty property;
        public string propertyName;
        public int propertyID;
        public ExposedPropertyWrapper Clone() {
        var prop = new ExposedPropertyWrapper();
        prop.propertyName = propertyName;
        prop.property = ScriptableObject.Instantiate(property);
        prop.property.value = property.value;
        prop.property.name = property.name;
        prop.propertyID = property.name.GetHashCode();
        return prop;
    }
    }
    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(ExposedPropertyWrapper))]
    internal class ExposedPropertyWrapperDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            var propertyProp = property.FindPropertyRelative("property");
            var propertyNameProp = property.FindPropertyRelative("propertyName");
            var propertyRect = new Rect(position.x, position.y, 150, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.width - 60, position.y, 100, EditorGUIUtility.singleLineHeight);
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(propertyRect, propertyNameProp, GUIContent.none);
            if (EditorGUI.EndChangeCheck()) {
                (propertyProp.objectReferenceValue as Parameter)?.ChangeParameterName(propertyNameProp.stringValue);
            }
            if (propertyProp != null && propertyProp.objectReferenceValue != null) {
                var propertySO = new SerializedObject(propertyProp.objectReferenceValue as Parameter);
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(valueRect, propertySO.FindProperty("value"), GUIContent.none);
                if (EditorGUI.EndChangeCheck()) {
                    propertySO.ApplyModifiedProperties();
                }
            }
        }
    }
    #endif
}
