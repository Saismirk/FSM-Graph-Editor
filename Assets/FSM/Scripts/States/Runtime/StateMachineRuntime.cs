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
        
        public StateMachineController controllerInstance;
        private void Start() {
            if (controller == null) return;
            controllerInstance = controller.Clone();  
            if (EditorWindow.HasOpenInstances<StateMachineGraphWindow>()){
                var window = EditorWindow.GetWindow<StateMachineGraphWindow>();
                if (window.controller == controller) {
                    window.ReloadGraph(controllerInstance);
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
        public void SetFloat(string parameter, float value) => controllerInstance.GetParameter<FloatParameter>(parameter).value.FloatValue = value;
        public void SetInt(string parameter, int value) => controllerInstance.GetParameter<IntParameter>(parameter).value.IntValue = value;
        public void SetBool(string parameter, bool value) => controllerInstance.GetParameter<BoolParameter>(parameter).value.BoolValue = value;
        public float GetFloat(string parameter) => controllerInstance.GetParameter<FloatParameter>(parameter).value.FloatValue;
        public int GetInt(string parameter) => controllerInstance.GetParameter<IntParameter>(parameter).value.IntValue;
        public bool GetBool(string parameter) => controllerInstance.GetParameter<BoolParameter>(parameter).value.BoolValue;
    }
}