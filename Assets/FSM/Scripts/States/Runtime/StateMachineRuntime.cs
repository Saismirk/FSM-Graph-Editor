using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using FSM.Graph;
namespace FSM {
    public class StateMachineRuntime : MonoBehaviour {
        public StateMachineController controller;
        public string activeState;
        public bool selected;
        public static event System.Action<string> OnParameterChanged;
        public StateMachineController controllerInstance;
        private void Start() {
            if (controller == null) return;
            controllerInstance = controller.Clone(this);  
            if (EditorWindow.HasOpenInstances<StateMachineGraphWindow>()){
                var window = EditorWindow.GetWindow<StateMachineGraphWindow>();
                if (window.controller == controller) {
                    StateMachineGraphWindow.OpenGraphWindow(controllerInstance);
                    selected = true;
                }
            }          
            controllerInstance.SetState(controllerInstance.GetEntryState());
        }
        private void OnEnable() {
            StateMachineGraphWindow.OnControllerSelected += OnControllerChanged;
        }
        private void OnDisable() {
            StateMachineGraphWindow.OnControllerSelected -= OnControllerChanged;
        }
        void OnControllerChanged(StateMachineController newController) {
            if (controllerInstance == null) return;
            selected = newController == controllerInstance;
        }
        private void OnApplicationQuit() {
            controller?.InitalizeControllers(controller);
            if (EditorWindow.HasOpenInstances<StateMachineGraphWindow>()){
                var window = EditorWindow.GetWindow<StateMachineGraphWindow>();
                if (window.controller == controllerInstance) {
                    window.ReloadGraph(controller);
                }
            }
        }
        private void Update() {
            activeState = controllerInstance.CurrentState == null ? "No State" : controllerInstance.CurrentState.stateName;
            controllerInstance?.UpdateCurrentState(this, selected);
        }
        public void SetFloat(string parameter, float value) {
            var param = controllerInstance.GetParameter<FloatParameter>(parameter);
            if (param.value.FloatValue != value) {
                param.value.FloatValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetInt(string parameter, int value) {
            var param = controllerInstance.GetParameter<IntParameter>(parameter);
            if (param.value.IntValue != value) {
                param.value.IntValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetBool(string parameter, bool value) {
            var param = controllerInstance.GetParameter<BoolParameter>(parameter);
            if (param.value.BoolValue != value) {
                param.value.BoolValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public float GetFloat(string parameter) => controllerInstance.GetParameter<FloatParameter>(parameter).value.FloatValue;
        public int GetInt(string parameter) => controllerInstance.GetParameter<IntParameter>(parameter).value.IntValue;
        public bool GetBool(string parameter) => controllerInstance.GetParameter<BoolParameter>(parameter).value.BoolValue;
    }
}