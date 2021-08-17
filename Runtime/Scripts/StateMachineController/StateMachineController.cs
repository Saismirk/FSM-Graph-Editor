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
        public List<ExposedPropertyWrapper> properties = new List<ExposedPropertyWrapper>();
        internal List<AnyState> anyStates = new List<AnyState>();
        public string[] listOfParameters;
        public State CurrentState {get; private set;}
        public State PreviousState {get; private set;}
        StateMachineRuntime runtime;
        Dictionary<int, int> parameterHashSet = new Dictionary<int, int>();
        Dictionary<int, int> propertyHashSet = new Dictionary<int, int>();
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
        public static void CreateAsset() {
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
            asset.CreateState(typeof(ExitState), new Vector2(400, 0), false);
            asset.CreateState(typeof(AnyState), new Vector2(0, 300), false);
        }
        public StateMachineController Clone(StateMachineRuntime stateMachine) {
            var controller = Instantiate(this);
            controller.Init(stateMachine);
            parameters.ForEach(parameter => {
                controller.parameters.Add(parameter.Clone());
            });
            controller.parameterHashSet = new Dictionary<int, int>();
            int i = 0;
            parameters.ForEach(p => {
                controller.parameterHashSet.Add(p.parameterID, i);
                i++;
            });
            InitalizeControllers(controller);
            return controller;
        }
        internal override string GetPath(bool includeSelf) {
            return includeSelf ? name : "";
        }
        public void Init(StateMachineRuntime stateMachine) {
            runtime = stateMachine;
            parameters = new List<ParameterWrapper>();
        }

        public void OnEnable() {
            var path = AssetDatabase.GetAssetPath(this);
            if (string.IsNullOrEmpty(path)) return;
            if (!states.Any(s => s is EntryState)){
                CreateState(typeof(EntryState), default, false);
            }
            if (!states.Any(s => s is ExitState)){
                CreateState(typeof(ExitState), new Vector2(400, 0), false);
            }
            if (!states.Any(s => s is AnyState)){
                CreateState(typeof(AnyState), new Vector2(0, 300), false);
            }
        }
        public void UpdateCurrentState(StateMachineRuntime stateMachine, bool updateCallback) {
            CurrentState?.OnStateUpdate(stateMachine);
            if (updateCallback) OnStateUpdated?.Invoke(CurrentState, true);
            if (CurrentState.LockedState) return;
            if(CurrentState.CheckTransitions()) return;

            anyStates?.ForEach(anyState => {
                if (anyState.CheckTransitions()) return;
            });
            var subState = CurrentState.Owner as SubStateMachineController;
            subState?.exitState.CheckTransitions();
        }



        
        public void SetFloat(int parameter, float value) => GetParameter<FloatParameter>(parameter).value.FloatValue = value;
        public void SetInt(int parameter, int value) => GetParameter<IntParameter>(parameter).value.IntValue = value;
        public void SetBool(int parameter, bool value) => GetParameter<BoolParameter>(parameter).value.BoolValue = value;
        public float GetFloat(int parameter) => GetParameter<FloatParameter>(parameter).value.FloatValue;
        public int GetInt(int parameter) => GetParameter<IntParameter>(parameter).value.IntValue;
        public bool GetBool(int parameter) => GetParameter<BoolParameter>(parameter).value.BoolValue;

        List<Parameter> GetParametersOfType<T>() where T : Parameter {
            var listOfTypeMatchParams = new List<Parameter>();
            parameters.Where(wrapper => wrapper.parameter is T).ToList().ForEach(parameter => {
                listOfTypeMatchParams.Add(parameter.parameter);
            });
            return listOfTypeMatchParams;
        }
        List<ExposedProperty> GetPropertiesOfType<T>() where T : ExposedProperty {
            var listOfTypeMatchProps = new List<ExposedProperty>();
            properties.Where(wrapper => wrapper.property is T).ToList().ForEach(wrapper => {
                listOfTypeMatchProps.Add(wrapper.property);
            });
            return listOfTypeMatchProps;
        }
        public Parameter GetParameter<T>(int parameterHash) where T : Parameter {
            var index = -1;
            parameterHashSet.TryGetValue(parameterHash, out index);
            return index >= 0 ? parameters[index].parameter : null;
        }
        public ExposedProperty GetProperty<T>(int propertyHash) where T : ExposedProperty {
            var index = -1;
            if (propertyHashSet.Count > 0) propertyHashSet.TryGetValue(propertyHash, out index);
            if (!Application.isPlaying) {
                index = properties.FindIndex(p => p.property.name.GetHashCode() == propertyHash);
            }
            //Debug.Log($"Getting property {propertyHash} with index {index}: {(index >= 0 ? properties[index].property : null)}");
            return index >= 0 ? properties[index].property : null;
        }
        public void AddParameter(Parameter param) {
            Undo.RecordObject(this, "Creation (Parameter)");
            param.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(param, this);
            Undo.RegisterCreatedObjectUndo(param, "Creation (Parameter)");
            GenerateListOfParameters();
        }
        public void AddProperty(ExposedProperty prop) {
            Undo.RecordObject(this, "Creation (Property)");
            prop.hideFlags = HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(prop, this);
            Undo.RegisterCreatedObjectUndo(prop, "Creation (Property)");
        }
        public void RemoveParameter(Parameter param) {
            Undo.RecordObject(this, "Deletion (Parameter)");
            Undo.DestroyObjectImmediate(param);
            GenerateListOfParameters();
        }
        public override void GenerateListOfParameters() {
            if (Application.isPlaying) return;
            var list = new List<string>();
            parameters.ForEach(p => {
                if (p.parameter != null) list.Add(p.parameter.name);
            });
            listOfParameters = list.ToArray();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        public List<SubStateMachineController> GetAllSubStates() {
            var subStatesInController = new List<SubStateMachineController>();

            return subStatesInController;
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