using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
namespace FSM {
    [System.Serializable]
    [CreateAssetMenu(menuName = "StateMachine/Transition")]
    public class StateTransition : ScriptableObject {
        public State stateToTransition;
        public List<Condition> conditions = new List<Condition>();
        [SerializeField] StateMachineController controller;
        public void Init(StateMachineController controller) {
            this.controller = controller;
        }
        public bool CheckTransition(out State state) {
            state = null;
            foreach (var condition in conditions) {
                if (!condition.CheckCondition()) return false;
            }
            state = stateToTransition;
            return true;
        }

        #if UNITY_EDITOR
        [CustomEditor(typeof(StateTransition))]
        public class StateTransitionEditor : Editor {
            StateTransition Controller => target as StateTransition;
            ReorderableList conditionsList;
            SerializedProperty conditionsProp;
            private void OnEnable() {
                GenerateConditionsList();
            }
            void GenerateConditionsList() {
                conditionsProp = serializedObject.FindProperty("conditions");
                conditionsList = new ReorderableList(serializedObject, conditionsProp, true, true, true, true);
                conditionsList.drawElementCallback = (rect, index, isActive, isFocused) => {
                    var parameter = conditionsList.serializedProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, parameter, GUIContent.none);
                };
                conditionsList.drawHeaderCallback = rect => {
                    EditorGUI.LabelField(rect, "Parameters");
                };
                conditionsList.onAddCallback = list => {
                    var index = list.serializedProperty.arraySize;
                    conditionsProp.arraySize++;
                    list.index = index;
                    var element = list.serializedProperty.GetArrayElementAtIndex(index);
                    var newCondition = new Condition();
                    newCondition.controller = Controller.controller;
                    serializedObject.ApplyModifiedProperties();
                };
            }
            public override void OnInspectorGUI() {
                Controller.conditions.ForEach(condition => {
                    condition.Init(Controller.controller);
                });
                EditorGUILayout.LabelField($"Transition To '{Controller.stateToTransition.stateName}'", EditorStyles.boldLabel);
                serializedObject.Update();
                conditionsList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();
            }
        }

        [CustomPropertyDrawer(typeof(StateTransition))]
        public class StateTransitionDrawer : PropertyDrawer {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
                var serializedObject = new SerializedObject(property.objectReferenceValue as StateTransition);
                var transitionProp = serializedObject.FindProperty("stateToTransition"); 
                var labelRect = new Rect(position.x, position.y, position.width * 0.75f, EditorGUIUtility.singleLineHeight);
                var buttonRect = new Rect(position.x + position.width * 0.75f + 10, position.y, position.width * 0.2f, EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(labelRect, $"To '{(transitionProp?.objectReferenceValue as State)?.stateName}'");
                if (serializedObject?.targetObject != null && GUI.Button(buttonRect, "Select")) {
                    Selection.activeObject = serializedObject.targetObject;
                }
            }
        }
        #endif
    }
}