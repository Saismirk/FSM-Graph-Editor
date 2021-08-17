using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using UnityEditorInternal;
#endif
namespace FSM.Graph {
    public class StateMachineGraphWindow : GraphViewEditorWindow {
        public StateMachineController controller;
        StateMachineGraphView graphView;
        ReorderableList parameterList;
        VisualElement mainContainer;
        VisualElement root;
        VisualElement subLayerContainer;
        Label graphTitleLabel;
        ToolbarButton inspectorButton;
        ToolbarButton parameterButton;
        ToolbarMenu subStateMenu;
        SerializedProperty parameterProp;
        static SerializedObject serializedObject;
        private Toolbar toolbar;
        private Edge selectedEdge;
        Vector2 currentSize;
        private Button blackboardButton;

        bool CanvasHasResized {
            get {
                if (currentSize != position.size) {
                    currentSize = position.size;
                    return true;
                }
                return false;
            }
        }
        public static event System.Action<StateMachineController> OnControllerSelected;
        public static void OpenGraphWindow(StateMachineController controller) {
            var window = GetWindow<StateMachineGraphWindow>();
            window.controller = controller;
            window.titleContent = new GUIContent("State Machine Editor");
            window.InitializeWindow();
            window.GenerateGraphView();
            window.Show();
        }
        void OnEnable() {
            if (controller == null) return;
            serializedObject = new SerializedObject(controller);
            InitializeWindow();
        }
        void InitializeWindow(){
            rootVisualElement.Clear();
            var uxml = Resources.Load<VisualTreeAsset>("FSMEditor/UXML/StateMachineGraphWindow");
            serializedObject = new SerializedObject(controller);
            root = uxml.Instantiate();
            mainContainer = root.Q("MainContainer");
            subLayerContainer = root.Q("SubLayerContainer");
            graphTitleLabel = root.Q<Label>(name: "GraphTitle");
            graphTitleLabel.text = controller.name;
            inspectorButton = root.Q<ToolbarButton>(name: "InspectorButton");
            inspectorButton.style.backgroundColor = Color.white * 0.5f;
            inspectorButton.clicked += () => {
                graphView?.ToggleInspector();
                inspectorButton.style.backgroundColor = inspectorButton.style.backgroundColor != Color.clear ? Color.clear : Color.white * 0.5f;
            };
            parameterButton = root.Q<ToolbarButton>(name: "ParametersButton");
            parameterButton.style.backgroundColor = Color.white * 0.5f;
            parameterButton.clicked += () => {
                graphView?.ToggleBlackboard();
                parameterButton.style.backgroundColor = parameterButton.style.backgroundColor != Color.clear ? Color.clear : Color.white * 0.5f;
            };
            subStateMenu = root.Q<ToolbarMenu>(name: "SubStateMenu");
            rootVisualElement.Add(root);
            root.StretchToParentSize();
            AddSubStateLayerButton(controller);
            GenerateGraphView();
            AddSubStatesToMenu();
        }
        internal void AddSubStatesToMenu() {
            if (controller == null || subStateMenu == null) return;
            while (subStateMenu?.menu.MenuItems()?.Count > 0) {
                subStateMenu.menu.RemoveItemAt(0);
            }
            var subStates =  new List<StateMachineControllerBase>();
            subStates.Add(controller);
            controller.GetAndAddAllSubStates(ref subStates);
            subStates.Sort((s1,s2) => s1.depth.CompareTo(s2.depth));
            subStates.ForEach(subState => {
                subStateMenu?.menu.AppendAction(subState.GetPath(true), action => {
                    graphView?.ChangeController(subState);
                    PopulateToolbarLayers(subState);
                });
            });
        }
        internal void ReloadGraph(StateMachineController newController) {
            if (newController == null) return;
            controller = newController;
            
            graphView.Init(controller.parameters, controller.properties);
            OnControllerSelected?.Invoke(controller);
            serializedObject = new SerializedObject(controller);
            graphView?.ChangeController(controller);
            PopulateToolbarLayers(controller);
        }
        void OnGUI() {
            if (controller == null) {
                controller = Resources.Load<StateMachineController>("FSMEditor/DefaultStateMachineController");
                ReloadGraph(controller);
                return;
            }
            graphView?.UpdateGraphElementPositions();
            if (CanvasHasResized) graphView?.FitGraphElementsInCanvas(position);
            if (graphView?.selection.Count > 0) {
                var newSelection = graphView.selection[0] as Edge;
                if (selectedEdge == newSelection) return;
                selectedEdge = newSelection;
                if (selectedEdge != null && !selectedEdge.isGhostEdge) {
                    if (selectedEdge.input != null && selectedEdge.output != null) { 
                        if (!graphView.TransitionMap.TryGetValue(selectedEdge, out (State parent, State child) states)) return;
                        var input = states.child;
                        var output = states.parent;
                        graphView?.SetInspectorDisplay(output?.transitions?.First(t => t?.stateToTransition == input)); 
                    }
                }
            }
        }   
        private void OnSelectionChange() {
            if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent<StateMachineRuntime>(out var runtime)) {
                if (runtime.controllerInstance != controller) {
                    var c = !Application.isPlaying ? runtime.controller : runtime.controllerInstance;
                    ReloadGraph(c);
                    graphTitleLabel.text = c.name;
                }
                return;
            }
            if (Selection.activeObject as StateMachineController) {
                if (Selection.activeObject as StateMachineController != controller) {
                    var c = Selection.activeObject as StateMachineController;
                    ReloadGraph(c);
                    graphTitleLabel.text = c.name;
                }
                return;
            }
        }
        void GenerateGraphView() {
            graphView?.RemoveFromHierarchy();
            graphView = new StateMachineGraphView(this);
            graphView.controller = controller;
            graphView.main = controller;
            graphView.StretchToParentSize();
            graphView.Init(controller.parameters, controller.properties);
            mainContainer.Add(graphView);
            graphView.PopulateGraph();
        }
        public void AddSubStateLayerButton(StateMachineControllerBase newController){
            var newLayerButton = new Button(() => {
                graphView?.ChangeController(newController);
                PopulateToolbarLayers(newController);
            }){
                text = newController is StateMachineController ? " Main " : $" {newController.name} ",
            };
            newLayerButton.style.fontSize = 10;
            newLayerButton.style.borderTopLeftRadius = 6;
            newLayerButton.style.borderBottomRightRadius = 6;
            newLayerButton.style.borderBottomLeftRadius = 6;
            newLayerButton.style.borderTopRightRadius = 6;
            var sepLabel = new Label(">");
            sepLabel.style.fontSize = 10;
            if (newController is SubStateMachineController) subLayerContainer.Add(sepLabel);
            subLayerContainer.Add(newLayerButton);
        }
        public void PopulateToolbarLayers(StateMachineControllerBase newController) {
            if (subLayerContainer == null) return;
            subLayerContainer?.Clear();
            var parents = new List<StateMachineControllerBase>();
            newController.GetParents(ref parents);
            parents.Reverse();
            parents.ForEach(parent => {
                AddSubStateLayerButton(parent);
            });
            AddSubStateLayerButton(newController);
        }
    }
}