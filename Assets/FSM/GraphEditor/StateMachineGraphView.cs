
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
using FSM;
#endif
namespace FSM.Graph {
    public class StateMachineGraphView : GraphView {
        public StateMachineControllerBase controller;
        public StateMachineGraphWindow window;
        GraphNodeSearchWindow searchWindow;
        List<string> nodeCopyCache;
        string lastKnownGuid;
        public StateMachineGraphView(StateMachineGraphWindow window) {
            styleSheets.Add(Resources.Load<StyleSheet>("StateMachineEditor/StateMachineGraphStyle"));
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
        }
        private void OnDisable() {
            StateMachineController.OnStateUpdated -= SetNodeHighlight;
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

