using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    [System.Serializable]
    public class Condition {
        public enum IntConditionType { Greater, Less, Equal, NotEqual }
        public enum FloatConditionType { Greater, Less}
        public enum BoolConditionType { True, False }
        public StateMachineController controller;
        public Parameter parameter;
        public int selectedParamenterIndex;
        public ParameterValue value;
        public IntConditionType intConditionType;
        public FloatConditionType floatConditionType;
        public BoolConditionType boolConditionType;
        public void Init(StateMachineController controller) {
            this.controller = controller;
        }
        public bool CheckCondition() {
            //Debug.Log($"Checking condition for parameter {controller.parameters[selectedParamenterIndex].parameter}: {controller.parameters[selectedParamenterIndex].parameter.value.BoolValue}");
            bool result = controller.parameters[selectedParamenterIndex].parameter switch {
                BoolParameter b => boolConditionType switch {
                    BoolConditionType.True => (bool)b.GetValue(),
                    BoolConditionType.False => !(bool)b.GetValue(),
                    _ => false
                },
                FloatParameter f => floatConditionType switch {
                    FloatConditionType.Greater => (float)f.GetValue() > value.FloatValue,
                    FloatConditionType.Less => (float)f.GetValue() < value.FloatValue,
                    _ => false
                },
                IntParameter i => intConditionType switch {
                    IntConditionType.Greater => (int)i.GetValue() > value.IntValue,
                    IntConditionType.Less => (int)i.GetValue() < value.IntValue,
                    IntConditionType.Equal => (int)i.GetValue() == value.IntValue,
                    IntConditionType.NotEqual => (int)i.GetValue() != value.IntValue,
                    _ => false
                },
                _ => false
            };
            //Debug.Log($"Condition result {result}");
            return result;
        }
    }

    #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(Condition))]
    public class ConditionDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            //var parameterProp = property.FindPropertyRelative("parameter");
            var intTypeProp = property.FindPropertyRelative("intConditionType");
            var floatTypeProp = property.FindPropertyRelative("floatConditionType");
            var boolTypeProp = property.FindPropertyRelative("boolConditionType");
            var valueProp = property.FindPropertyRelative("value");
            var valueTypeProp = valueProp.FindPropertyRelative("type");
            var controllerProp = property.FindPropertyRelative("controller");
            SerializedProperty listParamNames = null;
            SerializedProperty listParams = null;
            if (controllerProp != null && controllerProp.objectReferenceValue != null) {
                var controllerSO = new SerializedObject(controllerProp.objectReferenceValue);
                listParamNames = controllerSO.FindProperty("listOfParameters");
                listParams = controllerSO.FindProperty("parameters");
            }
            var indexProp = property.FindPropertyRelative("selectedParamenterIndex");

            var parameterRect = new Rect(position.x, position.y, 150, EditorGUIUtility.singleLineHeight);
            var typeRect = new Rect(position.x + 160, position.y, 100, EditorGUIUtility.singleLineHeight);
            var valueRect = new Rect(position.x + 270, position.y, 100, EditorGUIUtility.singleLineHeight);
            var listParameters =  GetStringArray(listParamNames);
            if (listParameters == null) return;

            var index = indexProp.intValue;
            EditorGUI.BeginChangeCheck();
            index = EditorGUI.Popup(parameterRect, index, listParameters);
            if (EditorGUI.EndChangeCheck()) {
                indexProp.intValue = index;
                indexProp.serializedObject.ApplyModifiedProperties();
            }
            var selectedParameter = listParams.GetArrayElementAtIndex(index).FindPropertyRelative("parameter")?.objectReferenceValue as Parameter;

            if (selectedParameter != null) {
                switch (selectedParameter) {
                    case BoolParameter b :
                        EditorGUI.PropertyField(typeRect, boolTypeProp, GUIContent.none);
                        valueTypeProp.enumValueIndex = (int)ParameterValue.ParameterType.Bool;
                        break;
                    case FloatParameter f :
                        EditorGUI.PropertyField(typeRect, floatTypeProp, GUIContent.none);
                        valueTypeProp.enumValueIndex = (int)ParameterValue.ParameterType.Float;
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                        break;
                    case IntParameter i :
                        EditorGUI.PropertyField(typeRect, intTypeProp, GUIContent.none);
                        valueTypeProp.enumValueIndex = (int)ParameterValue.ParameterType.Int;
                        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);
                        break;
                }
                
            }
        }

        public string[] GetStringArray(SerializedProperty property) {
            if (property == null || !property.isArray) {
                return null;
            }
            var array = new string[property.arraySize];
            for (var i = 0; i < property.arraySize; i++) {
                array[i] = property.GetArrayElementAtIndex(i).stringValue;
            }
            return array;
        } 
    }
    #endif
}