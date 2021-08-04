using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
namespace FSM {
    [System.Serializable]
    [CreateAssetMenu()]
    public class State : ScriptableObject {
        public string stateName = "New State";
        public string guid;
        public Vector2 position;
        public List<StateTransition> transitions = new List<StateTransition>();
        public List<StateBehaviour> stateBehaviours =  new List<StateBehaviour>();
        [SerializeField] StateMachineController stateMachine;
        public void CheckTransitions() {
            foreach (var transition in transitions) {
                if (transition.CheckTransition(out State state)) { 
                    stateMachine.SetState(state);
                    return;
                }
            }
        }
        public void Init(StateMachineController stateMachine) {
            this.stateMachine = stateMachine;
        }

        public virtual void OnStateEnter(StateMachineRuntime runtime) {
            stateBehaviours.ForEach(behaviour => {
                behaviour?.OnEnter(runtime, stateMachine, this);
            });
        }
        public virtual void OnStateUpdate(StateMachineRuntime runtime) {
            stateBehaviours.ForEach(behaviour => {
                behaviour?.OnUpdate(runtime, stateMachine, this);
            });
        }
        public virtual void OnStateExit(StateMachineRuntime runtime) {
            stateBehaviours.ForEach(behaviour => {
                behaviour?.OnExit(runtime, stateMachine, this);
            });
        }

        public void AddTransition(State state) {
            Undo.RecordObject(this, "Creation (Transition)");
            var transition = CreateInstance<StateTransition>();
            transition.hideFlags = HideFlags.HideInHierarchy;
            transition.name = $"Transition: {stateName} - {state.stateName}";
            transition.stateToTransition = state;
            transition.Init(stateMachine);
            transitions.Add(transition);
            AssetDatabase.AddObjectToAsset(transition, this);
            Undo.RegisterCreatedObjectUndo(transition, "Creation (Transition)");
            AssetDatabase.SaveAssets();
        }
        public void RemoveTransition(StateTransition transition) {
            Undo.RecordObject(this, "Deletion (Transition)");
            transitions.Remove(transition);
            Undo.DestroyObjectImmediate(transition);
            AssetDatabase.SaveAssets();
        }

        void AddBehaviour(StateBehaviour newBehaviour) {
            Undo.RecordObject(this, "Creation (State Behaviour)");
            newBehaviour.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(newBehaviour, this);
            Undo.RegisterCreatedObjectUndo(newBehaviour, "Creation (State Behaviour)");
            AssetDatabase.SaveAssets();
        }

        void RemoveBehaviour(StateBehaviour newBehaviour) {
            Undo.RecordObject(this, "Deletion (State Behaviour)");
            if (newBehaviour == null) return;
            Undo.DestroyObjectImmediate(newBehaviour);
            AssetDatabase.SaveAssets();
        }
        [ContextMenu("Destroy All Behaviours")]
        public void DestroyAllBehaviours(){
            var assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(this)).ToList();
            assets.ForEach(asset => {
                if (asset is StateBehaviour) {
                    Debug.Log(asset.name);
                    AssetDatabase.RemoveObjectFromAsset(asset);
                }
            });
        }


        #if UNITY_EDITOR
        [CustomEditor(typeof(State))]
        public class StateEditor : Editor {
            State Controller => target as State;
            ReorderableList behaviourList;
            SerializedProperty behaviourProp;
            private void OnEnable() {
                behaviourProp = serializedObject.FindProperty("stateBehaviours");
                behaviourList = new ReorderableList(serializedObject, behaviourProp, true, true, true, true);
                behaviourList.drawElementCallback = (rect, index, isActive, isFocused) => {
                    var behaviour = behaviourList.serializedProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, behaviour, GUIContent.none);
                };
                behaviourList.drawHeaderCallback = rect => {
                    EditorGUI.LabelField(rect, "State Behaviours");
                };
                behaviourList.onAddDropdownCallback = (rect, list) => {
                    var menu = new GenericMenu();
                    var behaviourTypes = TypeCache.GetTypesDerivedFrom<StateBehaviour>().ToList();
                    behaviourTypes.ForEach(type => {
                        menu.AddItem(new GUIContent(type.Name), false, BehaviourCreationHandler, type);
                    });
                    menu.ShowAsContext();
                };
                behaviourList.onRemoveCallback = list => {
                    var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
                    Controller.RemoveBehaviour(element.objectReferenceValue as StateBehaviour);
                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                    serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(this);
                };
            }

            void BehaviourCreationHandler(object targetObject) {
                var type = targetObject as System.Type;
                var index = behaviourList.serializedProperty.arraySize;
                behaviourProp.arraySize++;
                behaviourList.index = index;
                var element = behaviourList.serializedProperty.GetArrayElementAtIndex(index);
                var newBehaviour = CreateInstance(type) as StateBehaviour;
                newBehaviour.name = type.Name;
                Controller.AddBehaviour(newBehaviour);
                element.objectReferenceValue = newBehaviour;
                serializedObject.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                EditorUtility.SetDirty(this);
            }

            public override void OnInspectorGUI() {
                var transitionsProp = serializedObject.FindProperty("transitions");
                var stateNameProp = serializedObject.FindProperty("stateName");
                EditorGUILayout.PropertyField(stateNameProp);
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(transitionsProp);
                if (EditorGUI.EndChangeCheck()) {
                    Controller.transitions.ForEach(transition => {
                        transition.Init(Controller.stateMachine);
                    });
                }
                serializedObject.Update();
                behaviourList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();
            }
        }
        #endif
    }

    [System.Serializable]
    public class StateData {
        public string stateName;
        public List<StateBehaviour> behaviours;
        public Vector2 position;
        public StateData(State state) {
            stateName = state.stateName;
            behaviours = state.stateBehaviours.ToList();
            position = state.position;
        }
    }
}

