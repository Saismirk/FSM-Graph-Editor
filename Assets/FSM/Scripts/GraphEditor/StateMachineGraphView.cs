
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using FSM;
#endif
namespace FSM.Graph {
    public class StateMachineGraphView : GraphView {
        public StateMachineControllerBase controller;
        public List<ParameterWrapper> parameters =  new List<ParameterWrapper>();
        List<VisualElement> parameterElements = new List<VisualElement>();
        List<BlackboardRow> parameterRows = new List<BlackboardRow>();
        public StateMachineGraphWindow window;
        GraphNodeSearchWindow searchWindow;
        List<string> nodeCopyCache;
        string lastKnownGuid;
        private Blackboard blackboard;
        private BlackboardSection parameterSection;
        VisualElement eventTarget;

        public StateMachineGraphView(StateMachineGraphWindow window) {
            styleSheets.Add(Resources.Load<StyleSheet>("FSMEditor/StateMachineGraphStyle"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            
            this.window = window;
            AddSearchWindow();

            StateMachineController.OnStateUpdated += SetNodeHighlight;
            Undo.undoRedoPerformed += () => {
                PopulateGraph();
                AssetDatabase.SaveAssets();
            };
            serializeGraphElements += (elements) => {
                var listNodes = elements?.Where(e => e is StateMachineNode)?.Cast<StateMachineNode>().ToList();
                var listStates = new List<StateData>();
                nodeCopyCache = new List<string>();
                listNodes.ForEach(node => {
                    listStates.Add(new StateData(node.state));
                });
                listStates.ForEach(data => {
                    nodeCopyCache.Add(JsonUtility.ToJson(data));
                });
                return JsonUtility.ToJson(nodeCopyCache);
            };
            unserializeAndPaste += (string operation, string data) => {
                if (nodeCopyCache.Count == 0) return;
                nodeCopyCache.ForEach(cache => {
                    var state = JsonUtility.FromJson<StateData>(cache);
                    var newState =  CreateNode(typeof(State), state.position + Vector2.one * 20);
                    newState.stateName = state.stateName;
                    newState.stateBehaviours = state.behaviours.ToList();
                });
                AssetDatabase.SaveAssets();
            };
            StateMachineRuntime.OnParameterChanged += ParameterChangeHandler;
        }
        public void Init(List<ParameterWrapper> wrappers) {
            parameters = wrappers;
            GenerateBlackboard();
        }
        void ParameterChangeHandler(string paramName) {
            var index = parameters.FindIndex(p => p.parameterName == paramName);         
            switch (parameterElements[index]){
                case Toggle t:
                    t.value = parameters[index].parameter.value.BoolValue;
                    break;
                case FloatField t:
                    t.value = parameters[index].parameter.value.FloatValue;
                    break;
                case IntegerField t:
                    t.value = parameters[index].parameter.value.IntValue;
                    break;
            }

        }
        void GenerateBlackboard() {
            var rect = new Rect(20, 30, 250, 300);
            if (blackboard != null) {
                rect = blackboard.GetPosition();
                Remove(blackboard);
            }
            blackboard = new Blackboard(this){
                title = "Parameters",
                subTitle = "",
                scrollable = true,
            };
            parameterSection = new BlackboardSection(){
                title = "Parameters"
            };
            int i = 0;
            parameters.ForEach(param => {
                AddParameterToBlackboard(param, i);
                i++;
            });
            blackboard.RegisterCallback<ContextualMenuPopulateEvent>(evt => {
                eventTarget = evt.target as VisualElement;
                evt.menu.AppendAction("Delete", (action) => {
                    DeleteParameterFromBlackboard((int)eventTarget.userData);
                    GenerateBlackboard();
                });
            });
            blackboard.Add(parameterSection);
            blackboard.SetPosition(rect);
            blackboard.addItemRequested = bb => { 
                if (Application.isPlaying) {
                    EditorUtility.DisplayDialog("Warning", "Cannot add parameters during runtime.", "OK");
                    return;
                }
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Float Parameter"), false, ParameterCreationHandler<FloatParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Int Parameter"), false, ParameterCreationHandler<IntParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Bool Parameter"), false, ParameterCreationHandler<BoolParameter>, new ParameterWrapper()); 
                menu.ShowAsContext();
            };
            blackboard.editTextRequested = (blackboard, element, newName) => {
                if (Application.isPlaying) {
                    EditorUtility.DisplayDialog("Warning", "Cannot rename parameters during runtime.", "OK");
                    return;
                }
                var oldName = ((BlackboardField)element).text;
                if (parameters.Any(p => p.parameterName == newName)){
                    EditorUtility.DisplayDialog("Warning", "Parameter name already exists.", "OK");
                    return;
                }
                var paramIndex = parameters.FindIndex(p => p.parameterName ==  oldName);
                if (paramIndex < 0) return;
                Undo.RecordObject(parameters[paramIndex].parameter, "Rename (Parameter)");
                parameters[paramIndex].parameterName = newName;
                parameters[paramIndex].parameter.name = newName;
                ((BlackboardField)element).text = newName;
                AssetDatabase.SaveAssets();
            };
            Add(blackboard);
        }
        void DeleteParameterFromBlackboard(int index) {
            parameters.RemoveAt(index);
            parameterElements.RemoveAt(index);
            AssetDatabase.SaveAssets();
        }
        void AddParameterToBlackboard(ParameterWrapper param, int index = 0){
            var blackboardField = new BlackboardField(){
                text = param.parameterName,
                typeText = ObjectNames.NicifyVariableName(param.parameter.GetType().ToString()),
            };
            blackboardField.userData = index;
            
            parameterSection.Add(blackboardField);
            switch (param.parameter) {
                case BoolParameter p:
                    var boolValue = new Toggle(){value = param.parameter.value.BoolValue, label = "Value: "};
                    boolValue.RegisterValueChangedCallback(evt => {
                        param.parameter.value.BoolValue = evt.newValue;
                    });
                    parameterElements.Add(boolValue);
                    parameterSection.Add(new BlackboardRow(blackboardField, boolValue));
                    break;
                case FloatParameter p:
                    var floatValue = new FloatField(){value = param.parameter.value.FloatValue, label = "Value: "};
                    floatValue.RegisterValueChangedCallback(evt2 => {
                        param.parameter.value.FloatValue = evt2.newValue;
                    });
                    parameterElements.Add(floatValue);
                    parameterSection.Add(new BlackboardRow(blackboardField, floatValue));
                    break;
                case IntParameter p:
                    var intValue = new IntegerField(){value = param.parameter.value.IntValue, label = "Value: "};
                    intValue.RegisterValueChangedCallback(evt3 => {
                        param.parameter.value.IntValue = evt3.newValue;
                    });
                    parameterElements.Add(intValue);
                    parameterSection.Add(new BlackboardRow(blackboardField, intValue));
                    break;
            }
            parameterSection.name = param.parameterName;
        }

        void ParameterCreationHandler<T>(object targetObject) where T : Parameter {
            var newParameter = ScriptableObject.CreateInstance<T>();
            newParameter.name = typeof(T).Name;
            (controller as StateMachineController).AddParameter(newParameter);
            var newWrapper = new ParameterWrapper(){
                parameter = newParameter,
                parameterName = newParameter.name
            };
            (controller as StateMachineController).parameters.Add(newWrapper);
            AssetDatabase.SaveAssets();
            GenerateBlackboard();
        }

        private void OnDisable() {
            StateMachineController.OnStateUpdated -= SetNodeHighlight;
            StateMachineRuntime.OnParameterChanged -= ParameterChangeHandler;
        }
        
        void AddSearchWindow() {
            searchWindow = ScriptableObject.CreateInstance<GraphNodeSearchWindow>();
            searchWindow.Init(this, window);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }
        public State CreateNode(System.Type type, Vector2 position = default) {
            var state = controller.CreateState(type, position);
            AddElement(new StateMachineNode(state));
            return state;
        }

        public void CreateSubStateMachine(System.Type type, Vector2 position = default){
            var subStateMachine = controller.CreateSubStateMachine(type, position);
            AddElement(new SubStateMachineNode(subStateMachine, this));
        }
        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();
            ports.ForEach(port => {
                if(startPort != port && startPort.node != port.node && port.direction != startPort.direction) {
                    compatiblePorts.Add(port);
                }
            });
            return compatiblePorts;
        }

        public void ChangeController(StateMachineControllerBase newController) {
            controller = newController;
            PopulateGraph();
        }

        public void PopulateGraph() {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;

            controller?.states?.ForEach(node => AddElement(new StateMachineNode(node)));
            controller?.subStates?.ForEach(node => AddElement(new SubStateMachineNode(node, this)));

            controller?.states?.ForEach(node => {
                var children = controller.GetTransitions(node);
                var parentView = GetNodeByGuid(node.guid) as GraphNode;

                foreach (var child in children) {
                    var childView = GetNodeByGuid(child.stateToTransition.guid) as GraphNode;
                    if (childView == null) continue;
                    var edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge);
                }
            });
            controller?.subStates?.ForEach(node => {
                var children = node.GetTransitions(node.GetExitState());
                var parentView = GetNodeByGuid(node.guid) as GraphNode;
                children.ForEach(child => {
                    var childView = GetNodeByGuid(child.stateToTransition.guid) as GraphNode;
                    var edge = parentView.output.ConnectTo(childView.input);
                    AddElement(edge); 
                });
            });
        }

        public void SetNodeHighlight(State state, bool set = false) {
            //Debug.Log($"Highlighting node with GUID: {state.guid}");
            var node = state != null ? GetNodeByGuid(state.guid) : null;
            if (node == null) {
                node = GetNodeByGuid(lastKnownGuid);
                if (node == null) return;
            }
            else if (set) lastKnownGuid = state.guid;
            node.style.backgroundColor = set ? new Color(0.75f, 0.75f, 1, 0.8f) : Color.clear;
        }
        GraphViewChange OnGraphViewChanged (GraphViewChange change) {
            if (change.elementsToRemove != null) {
                change.elementsToRemove.ForEach(element => {
                    if (element is BlackboardField field) {
                        DeleteParameterFromBlackboard((int)element.userData);
                        GenerateBlackboard();
                    }
                    if (element is StateMachineNode graphNode) {
                        controller.DeleteState(graphNode.state);
                    }
                    if (element is SubStateMachineNode subStateNode) {
                        controller.DeleteSubStateMachine(subStateNode.subStateMachine);
                    }
                    if (element is Edge edge) {
                        var parent = edge.output.node;
                        var child = edge.input.node;
                        controller.DeleteChild(
                            parent is StateMachineNode ? (parent as StateMachineNode).state : (parent as SubStateMachineNode).subStateMachine.GetExitState(),
                            child is StateMachineNode ? (child as StateMachineNode).state : (child as SubStateMachineNode).subStateMachine.GetEntryState()
                        );
                    }
                });
            }
            if (change.edgesToCreate != null) {
                change.edgesToCreate.ForEach(edge => {
                    var parent = edge.output.node;
                    var child = edge.input.node;

                    controller.AddChild(
                        parent is StateMachineNode ? (parent as StateMachineNode).state : (parent as SubStateMachineNode).subStateMachine.GetExitState(),
                        child is StateMachineNode ? (child as StateMachineNode).state : (child as SubStateMachineNode).subStateMachine.GetEntryState()
                    );
                });
            }
            return change;
        }
    }
}

