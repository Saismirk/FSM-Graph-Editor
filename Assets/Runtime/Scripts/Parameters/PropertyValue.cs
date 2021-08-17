using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
[Serializable] public struct TransformData : IEquatable<TransformData> {
    public Vector3 position, scale, rotation;
    public TransformData(Transform transform) {
        position = transform.position;
        scale = transform.localScale;
        rotation = transform.eulerAngles;
    }
    public bool Equals(TransformData other) {
        return position == other.position && scale == other.scale && rotation == other.rotation;
    }
    public static bool operator ==(TransformData a, TransformData b) => a.Equals(b);
    public static bool operator !=(TransformData a, TransformData b) => !a.Equals(b);
}

[Serializable]
public class PropertyValue {
    public enum PropertyType{Transform, Vector4, Vector3, Vector2, Float, Bool, Color, GameObject}
    public PropertyType type;
    public TransformData TransformValue; 
    public Vector4 Vector4Value;
    public Vector3 Vector3Value;
    public Vector2 Vector2Value;
    public float FloatValue;
    public bool BoolValue;
    public Color ColorValue;
    public GameObject GameObjectValue;
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(PropertyValue))]
public class PropertyValueDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var typeProp = property.FindPropertyRelative("type").enumValueIndex;
        var valueProp = (PropertyValue.PropertyType)typeProp switch {
            PropertyValue.PropertyType.Transform =>     property.FindPropertyRelative("TransformValue"),
            PropertyValue.PropertyType.Float =>         property.FindPropertyRelative("FloatValue"),
            PropertyValue.PropertyType.Vector2 =>       property.FindPropertyRelative("Vector2Value"),
            PropertyValue.PropertyType.Vector3 =>       property.FindPropertyRelative("Vector3Value"),
            PropertyValue.PropertyType.Vector4 =>       property.FindPropertyRelative("Vector4Value"),
            PropertyValue.PropertyType.Bool =>          property.FindPropertyRelative("BoolValue"),
            PropertyValue.PropertyType.Color =>         property.FindPropertyRelative("ColorValue"),
            PropertyValue.PropertyType.GameObject =>    property.FindPropertyRelative("GameObjectValue"),
            _ => null
        };
        if (valueProp != null) EditorGUI.PropertyField(position, valueProp, GUIContent.none);
    }
}
#endif