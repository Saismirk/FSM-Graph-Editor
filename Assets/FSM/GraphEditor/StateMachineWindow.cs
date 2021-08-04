using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
using FSM;
#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
#endif
namespace FSM.Graph {
    public class StateMachineGraphWindow : EditorWindow {
        public StateMachineController controller;
        StateMachineGraphView graphView;
        GUIStyle parameterPanelStyle;
        ReorderableList parameterList;
        SerializedProperty parameterProp;
        static SerializedObject serializedObject;
        private Toolbar toolbar;
        public static event System.Action<StateMachineController> OnControllerSelected;

        public static void OpenGraphWindow(StateMachineController controller, bool isRuntime = false) {
            var window = GetWindow<StateMachineGraphWindow>();
            window.controller = controller;
            window.titleContent = new GUIContent("State Machine Editor");
            serializedObject = new SerializedObject(controller);
            window.parameterPanelStyle = new GUIStyle();
            window.parameterPanelStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
            window.GenerateGraphView();
            window.GeneratePrameterList();
            window.GenerateToolbar();
        }
        void OnEnable() {
            if (controller == null) return;
            serializedObject = new SerializedObject(controller);
            parameterPanelStyle = new GUIStyle();
            parameterPanelStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
            GenerateGraphView();
            GeneratePrameterList();
            GenerateToolbar(); 
        }

        public void ReloadGraph(StateMachineController newController) {
            if (newController == null) return;
            controller = newController;
            OnControllerSelected?.Invoke(controller);
            serializedObject = new SerializedObject(controller);
            GeneratePrameterList();
            graphView?.ChangeController(controller);
            PopulateToolbarLayers(controller);
        }

        void OnGUI() {
            if (controller == null) {
                controller = Resources.Load<StateMachineController>("StateMachineEditor/DefaultStateMachineController");
                ReloadGraph(controller);
                return;
            }
            graphView.style.width = position.width - 305;
            DrawParameterPanel();
            if (graphView.selection.Count > 0) {
                var selection = graphView.selection[0] as Edge;
                if (selection != null) {
                    var input = selection.input.node is StateMachineNode
                        ? (selection.input.node as StateMachineNode).state
                        : (selection.input.node as SubStateMachineNode).subStateMachine.GetEntryState();
                    var output = selection.output.node is StateMachineNode
                        ? (selection.output.node as StateMachineNode).state
                        : (selection.output.node as SubStateMachineNode).subStateMachine.GetExitState();
                    Selection.activeObject = output.transitions.Where(t => t.stateToTransition == input).First();
                }
            }
        }   
        private void OnSelectionChange() {
            if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent<StateMachineRuntime>(out var runtime)) {
                if (runtime.controllerInstance != controller) {
                    ReloadGraph(!Application.isPlaying ? runtime.controller : runtime.controllerInstance);
                }
                return;
            }
            if (Selection.activeObject as StateMachineController) {
                if (Selection.activeObject as StateMachineController != controller) {
                    ReloadGraph(Selection.activeObject as StateMachineController);
                }
                return;
            }
        }

        void GenerateGraphView() {
            if (graphView != null && rootVisualElement.Contains(graphView)) rootVisualElement.Remove(graphView);
            graphView = new StateMachineGraphView(this);
            graphView.controller = controller;
            graphView.StretchToParentSize();
            graphView.style.width = position.width - 305;
            
            rootVisualElement.Add(graphView);
            graphView.PopulateGraph();
        }

        void GenerateToolbar() {
            if (rootVisualElement.Contains(toolbar)) {
                rootVisualElement.Remove(toolbar);
            }
            toolbar = new Toolbar();
            var mainButton = new Button(() => {
                graphView?.ChangeController(controller);
                PopulateToolbarLayers(controller);
            }){
                text = "Main",
            };
            toolbar.Add(mainButton);
            rootVisualElement.Add(toolbar);
        }

        public void AddSubStateLayerButton(StateMachineControllerBase newController){
            var newLayerButton = new Button(() => {
                graphView?.ChangeController(newController);
                PopulateToolbarLayers(newController);
            }){
                text = newController is StateMachineController ? "Main" : $"{newController.name} {newController.depth}",
            };
            toolbar.Add(newLayerButton);
        }

        public void PopulateToolbarLayers(StateMachineControllerBase newController) {
            toolbar.Clear();
            var parents = new List<StateMachineControllerBase>();
            newController.GetParents(ref parents);
            parents.Reverse();
            parents.ForEach(parent => {
                AddSubStateLayerButton(parent);
            });
            AddSubStateLayerButton(newController);
        }

        void DrawParameterPanel() {
            var paramentePanelRect = new Rect(position.width - 300, EditorGUIUtility.singleLineHeight, 300, position.height);
            var paramentePanelBorderRect = new Rect(position.width - 305, EditorGUIUtility.singleLineHeight, 5, position.height);
            GUILayout.BeginArea(paramentePanelRect);
            serializedObject.Update();
            EditorGUI.BeginChangeCheck();
            parameterList.DoLayoutList();
            if (EditorGUI.EndChangeCheck()) {
                controller.GenerateListOfParameters();
            }
            serializedObject.ApplyModifiedProperties(); 
            GUILayout.EndArea();

            GUILayout.BeginArea(paramentePanelBorderRect, parameterPanelStyle);
            GUILayout.EndArea();
        }

        void GeneratePrameterList() {
            parameterProp = serializedObject.FindProperty("parameters");
            parameterList = new ReorderableList(serializedObject, parameterProp, true, true, true, true);
            parameterList.drawElementCallback = (rect, index, isActive, isFocused) => {
                var parameter = parameterList.serializedProperty.GetArrayElementAtIndex(index);
                EditorGUI.PropertyField(rect, parameter, GUIContent.none);
            };
            parameterList.drawHeaderCallback = rect => {
                EditorGUI.LabelField(rect, "Parameters");
            };
            parameterList.onAddDropdownCallback = (rect, list) => {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Float Parameter"), false, ParameterCreationHandler<FloatParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Int Parameter"), false, ParameterCreationHandler<IntParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Bool Parameter"), false, ParameterCreationHandler<BoolParameter>, new ParameterWrapper());
                menu.ShowAsContext();
                controller.GenerateListOfParameters();
            };
            parameterList.onRemoveCallback = list => {
                var element = list.serializedProperty.GetArrayElementAtIndex(list.index);
                controller.RemoveParameter(element.FindPropertyRelative("parameter").objectReferenceValue as Parameter);
                ReorderableList.defaultBehaviours.DoRemoveButton(list);
                controller.GenerateListOfParameters();
            };
        }

        void ParameterCreationHandler<T>(object targetObject) where T : Parameter {
            var index = parameterList.serializedProperty.arraySize;
            parameterProp.arraySize++;
            parameterList.index = index;
            var element = parameterList.serializedProperty.GetArrayElementAtIndex(index);
            var newParameter = CreateInstance<T>();
            newParameter.name = typeof(T).Name;
            controller.AddParameter(newParameter);
            element.FindPropertyRelative("parameter").objectReferenceValue = newParameter;
            element.FindPropertyRelative("parameterName").stringValue = $"New {typeof(T).Name}";
            serializedObject.ApplyModifiedProperties();
        }
    }
}