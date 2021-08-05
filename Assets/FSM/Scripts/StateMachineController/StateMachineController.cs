using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FSM.Graph;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace FSM {
    public class StateMachineController : StateMachineControllerBase {
        public List<ParameterWrapper> parameters = new List<ParameterWrapper>();
        public string[] listOfParameters;
        public State CurrentState {get; private set;}
        public State PreviousState {get; private set;}
        StateMachineRuntime runtime;
        public static event System.Action<State, bool> OnStateUpdated;
        public virtual void SetState(State newState) {
            if(CurrentState?.guid == newState.guid) return;
            CurrentState?.OnStateExit(runtime);
            PreviousState = CurrentState;
            CurrentState = newState;
            CurrentState?.OnStateEnter(runtime);
            currentState = CurrentState.GetType().ToString();
            previousState = PreviousState?.GetType().ToString();
            OnStateUpdated?.Invoke(PreviousState, false);
        }

        [MenuItem("Assets/Create/State Machine Controller", false, 0)]
        public static void CreateMyAsset() {
            StateMachineController asset = CreateInstance<StateMachineController>();
            string path = AssetDatabase.GetAssetPath (Selection.activeObject);
            if (path == "") {
                path = "Assets";
            }
            else if (Path.GetExtension(path) != "") {
                path = path.Replace(Path.GetFileName (AssetDatabase.GetAssetPath (Selection.activeObject)), "");
            }
            path = AssetDatabase.GenerateUniqueAssetPath(path + "/New State Machine Controller.asset");

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            EditorUtility.FocusProjectWindow();

            Selection.activeObject = asset;
            asset.CreateState(typeof(EntryState), default, false);
            asset.CreateState(typeof(ExitState), new Vector2(300, 0), false);
        }
        public StateMachineController Clone() {
            var controller = Instantiate(this) as StateMachineController;
            controller.parameters = new List<ParameterWrapper>();
            parameters.ForEach(parameter => {
                controller.parameters.Add(parameter.Clone());
            });
            InitalizeControllers(controller);
            return controller;
        }
        public void OnEnable() {
            if (states.Count == 0){
                var path = AssetDatabase.GetAssetPath(this);
                if (string.IsNullOrEmpty(path)) return;
                CreateState(typeof(EntryState), default, false);
                CreateState(typeof(State), new Vector2(0, 300), false);
            }
        }
        public void UpdateCurrentState(StateMachineRuntime stateMachine, bool updateCallback) {
            CurrentState?.OnStateUpdate(stateMachine);
            if (updateCallback) OnStateUpdated?.Invoke(CurrentState, true);
            CurrentState?.CheckTransitions();
        }

        public void SetFloat(string parameter, float value) => GetParameter<FloatParameter>(parameter).value.FloatValue = value;
        public void SetInt(string parameter, int value) => GetParameter<IntParameter>(parameter).value.IntValue = value;
        public void SetBool(string parameter, bool value) => GetParameter<BoolParameter>(parameter).value.BoolValue = value;
        public float GetFloat(string parameter) => GetParameter<FloatParameter>(parameter).value.FloatValue;
        public int GetInt(string parameter) => GetParameter<IntParameter>(parameter).value.IntValue;
        public bool GetBool(string parameter) => GetParameter<BoolParameter>(parameter).value.BoolValue;

        List<Parameter> GetParameters<T> () where T : Parameter {
            var listOfTypeMatchParams = new List<Parameter>();
            parameters.Where(wrapper => wrapper.parameter is T).ToList().ForEach(parameter => {
                listOfTypeMatchParams.Add(parameter.parameter);
            });
            //Debug.Log($"Detected {listOfTypeMatchParams.Count} {typeof(T).Name}s.");
            return listOfTypeMatchParams;
        }

        public Parameter GetParameter<T>(string parameterName) where T : Parameter {
            return GetParameters<T>()?.Where(param => param.name == parameterName)?.First();
        }

        public void AddParameter(Parameter param) {
            Undo.RecordObject(this, "Creation (Parameter)");
            param.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(param, this);
            Undo.RegisterCreatedObjectUndo(param, "Creation (Parameter)");
            AssetDatabase.SaveAssets();
        }
        public void RemoveParameter(Parameter param) {
            Undo.RecordObject(this, "Deletion (Parameter)");
            Undo.DestroyObjectImmediate(param);
            AssetDatabase.SaveAssets();
        }
        public void GenerateListOfParameters() {
            var list = new List<string>();
            parameters.ForEach(p => {
                if (p.parameter != null) list.Add(p.parameter.name);
            });
            listOfParameters = list.ToArray();
        }
        [UnityEditor.Callbacks.OnOpenAsset]
        public static bool OpenGraphWindow(int instanceID, int value) {
            var asset = EditorUtility.InstanceIDToObject(instanceID) as StateMachineController;
            if (asset == null) return false;
            StateMachineGraphWindow.OpenGraphWindow(asset);
            return true;
        }

    #region Editor
    #if UNITY_EDITOR
        [CustomEditor(typeof(StateMachineController))]
        public class StateMachineControllerEditor : Editor {
            StateMachineController Controller => target as StateMachineController;
            public override void OnInspectorGUI() {
                if(GUILayout.Button("Open StateMachine Editor")) {
                    StateMachineGraphWindow.OpenGraphWindow(Controller);
                }
            }
        }
        #endif
    #endregion
    }
}