
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
#endif
namespace FSM.Graph {
    internal class StateMachineGraphView : GraphView {
        internal StateMachineControllerBase controller;
        internal StateMachineController main;
        internal List<ParameterWrapper> parameters =  new List<ParameterWrapper>();
        internal List<ExposedPropertyWrapper> properties =  new List<ExposedPropertyWrapper>();
        internal List<StateBehaviour> behaviours =  new List<StateBehaviour>();
        List<VisualElement> parameterElements = new List<VisualElement>();
        List<VisualElement> propertyElements = new List<VisualElement>();
        List<VisualElement> behaviourElements = new List<VisualElement>();
        internal StateMachineGraphWindow window;
        GraphNodeSearchWindow searchWindow;
        List<string> nodeCopyCache;
        string lastKnownGuid;
        private Blackboard blackboard;
        private BlackboardSection parameterSection, propertiesSection, behavioursSection;
        VisualElement eventTarget;
        private InspectorPanel inspectorPanel;
        Vector4 inspectorBorders, blackboardBorders;
        Vector2 blackboardPosition;
        private string currentSelectedGuid;
        const string pathToSettings = "Assets/Editor/FSMEditor/FSMSettings.asset";
        internal Dictionary<Edge, (State, State)> TransitionMap {get; private set;}

        bool BlackboardPositionChanged {
            get {
                var newPosition = blackboard.GetPosition().position;
                if (blackboard != null && blackboardPosition != newPosition){
                    blackboardPosition = newPosition;
                    return true;
                }
                return false;
            }
        }
        public StateMachineGraphView(StateMachineGraphWindow window) {
            styleSheets.Add(Resources.Load<StyleSheet>("FSMEditor/StateMachineGraphStyle"));
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 2);
            Insert(0, new GridBackground());
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            this.window = window;
            AddSearchWindow();

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
        internal void Init(List<ParameterWrapper> wrappers, List<ExposedPropertyWrapper> propertyWrappers, List<StateBehaviour> behaviourList) {
            parameters = wrappers;
            properties = propertyWrappers;
            behaviours = behaviourList;
            GenerateBlackboard();
            GenerateInspectorView();
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
        void GenerateInspectorView() {
            var rect = new Rect(window.position.width - 320, 50, 300, 400);
            if (inspectorPanel != null) {
                rect = inspectorPanel.GetPosition();
            }
            inspectorPanel?.RemoveFromHierarchy();
            inspectorPanel = new InspectorPanel();
            inspectorPanel.SetPosition(rect);
            inspectorPanel.style.position = Position.Absolute;
            Add(inspectorPanel);
        }
        public void SetInspectorDisplay (Object obj) {
            inspectorPanel.DisplayInspector(obj);
        }
        void GenerateBlackboard() {
            var rect = new Rect(20, 100, 300, 400);
            if (blackboard != null) {
                rect = blackboard.GetPosition();
                Remove(blackboard);
            }
            blackboard = new Blackboard(this){
                title = "Blackboard",
                subTitle = "",
                scrollable = true,
            };
            blackboard.style.backgroundColor = new Color(0.1686275f, 0.1686275f, 0.1686275f, 0.75f);
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
                    DeleteElementFromBlackboard((BlackboardFieldPropertyData)eventTarget.userData);
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
                menu.AddItem(new GUIContent("Parameters/Float"), false, ParameterCreationHandler<FloatParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Parameters/Int"), false, ParameterCreationHandler<IntParameter>, new ParameterWrapper());
                menu.AddItem(new GUIContent("Parameters/Bool"), false, ParameterCreationHandler<BoolParameter>, new ParameterWrapper());
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Properties/Float"), false, PropertyCreationHandler<FloatProperty>, new ExposedPropertyWrapper());
                menu.AddItem(new GUIContent("Properties/Bool"), false, PropertyCreationHandler<BoolProperty>, new ExposedPropertyWrapper());
                menu.AddItem(new GUIContent("Properties/Transform"), false, PropertyCreationHandler<TransformProperty>, new ExposedPropertyWrapper());
                menu.AddSeparator("");
                var behaviourPaths = TypeCache.GetTypesDerivedFrom<StateBehaviour>().ToList();
                behaviourPaths.ForEach(t => {
                    menu.AddItem(new GUIContent("Behaviours/" + t.Name), false, BehaviourCreationHandler, t);
                });
                menu.AddSeparator("Behaviours/");
                menu.AddItem(new GUIContent("Behaviours/Create New Behaviour"), false, CreateBehaviourHandler, null);
                menu.ShowAsContext();
            };
            blackboard.editTextRequested = (blackboard, element, newName) => {
                var data = element.userData as BlackboardFieldPropertyData;
                if (data == null) return;
                if (Application.isPlaying) {
                    EditorUtility.DisplayDialog("Warning", "Cannot rename parameters or properties during runtime.", "OK");
                    return;
                }
                var oldName = ((BlackboardField)element).text;
                switch (data.type) {
                    case BlackboardFieldPropertyData.FieldType.Parameter:
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
                        main.GenerateListOfParameters();
                        break;
                    case BlackboardFieldPropertyData.FieldType.Property:
                        if (properties.Any(p => p.propertyName == newName)){
                            EditorUtility.DisplayDialog("Warning", "Property name already exists.", "OK");
                            return;
                        }
                        var propIndex = properties.FindIndex(p => p.propertyName ==  oldName);
                        if (propIndex < 0) return;
                        Undo.RecordObject(properties[propIndex].property, "Rename (Property)");
                        properties[propIndex].propertyName = newName;
                        properties[propIndex].property.name = newName;
                        ((BlackboardField)element).text = newName;
                        break;
                }
                
                AssetDatabase.SaveAssets();
            };
            propertiesSection = new BlackboardSection(){
                title = "Properties"
            };
            i = 0;
            properties.ForEach(prop => {
                AddPropertyToBlackboard(prop, i);
                i++;
            });
            blackboard.Add(propertiesSection);
            behavioursSection = new BlackboardSection(){
                title = "Global Behaviours"
            };
            i = 0;
            behaviours.ForEach(b => {
                AddBehaviourToBlackboard(b, i);
                i++;
            });
            blackboard.Add(behavioursSection);
            Add(blackboard);
        }
        internal void ToggleBlackboard() {
            if (Contains(blackboard)) blackboard.RemoveFromHierarchy();
            else Add(blackboard);
        }
        internal void ToggleInspector() {
            if (Contains(inspectorPanel)) inspectorPanel.RemoveFromHierarchy();
            else Add(inspectorPanel);
        }
        void DeleteElementFromBlackboard(BlackboardFieldPropertyData data) {
            if (data == null) return;
            switch (data.type) {
                case BlackboardFieldPropertyData.FieldType.Parameter:
                    AssetDatabase.RemoveObjectFromAsset(parameters[data.index].parameter);
                    parameters.RemoveAt(data.index);
                    parameterElements.RemoveAt(data.index);
                    main.GenerateListOfParameters();
                    break;
                case BlackboardFieldPropertyData.FieldType.Property:
                    AssetDatabase.RemoveObjectFromAsset(properties[data.index].property);
                    properties.RemoveAt(data.index);
                    break;
                case BlackboardFieldPropertyData.FieldType.Behaviour:
                    AssetDatabase.RemoveObjectFromAsset(behaviours[data.index]);
                    behaviours.RemoveAt(data.index);
                    break;
            }           
            AssetDatabase.SaveAssets(); 
        }
        class BlackboardFieldPropertyData {
            internal enum FieldType {Parameter, Property, Behaviour};
            internal FieldType type;
            internal int index;

        }
        void AddParameterToBlackboard(ParameterWrapper param, int index = 0){
            var blackboardField = new BlackboardField(){
                text = param.parameterName,
                typeText = ObjectNames.NicifyVariableName(param.parameter.GetType().ToString()),
            };
            blackboardField.userData = new BlackboardFieldPropertyData(){
                type = BlackboardFieldPropertyData.FieldType.Parameter,
                index =  index
            };

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
            //parameterSection.name = param.parameterName;
        }
        void AddPropertyToBlackboard(ExposedPropertyWrapper prop, int index = 0){
            var blackboardField = new BlackboardField(){
                text = prop.propertyName,
                typeText = ObjectNames.NicifyVariableName(prop.property.GetType().Name),
            };
            blackboardField.userData = blackboardField.userData = new BlackboardFieldPropertyData(){
                type = BlackboardFieldPropertyData.FieldType.Property,
                index =  index
            };
            propertiesSection.Add(blackboardField);
            switch (prop.property) {
                case BoolProperty p:
                    var boolValue = new Toggle(){value = prop.property.value.BoolValue, label = "Value: "};
                    boolValue.RegisterValueChangedCallback(evt => {
                        prop.property.value.BoolValue = evt.newValue;
                    });
                    propertyElements.Add(boolValue);
                    parameterSection.Add(new BlackboardRow(blackboardField, boolValue));
                    break;
                case FloatProperty p:
                    var floatValue = new FloatField(){value = prop.property.value.FloatValue, label = "Value: "};
                    floatValue.RegisterValueChangedCallback(evt2 => {
                        prop.property.value.FloatValue = evt2.newValue;
                    });
                    propertyElements.Add(floatValue);
                    propertiesSection.Add(new BlackboardRow(blackboardField, floatValue));
                    break;
                case TransformProperty p:
                    var positionValue = new Vector3Field(){value = prop.property.value.TransformValue.position, label = "Position: "};
                    positionValue.RegisterValueChangedCallback(evt3 => {
                        prop.property.value.TransformValue.position = evt3.newValue;
                    });
                    var scaleValue = new Vector3Field(){value = prop.property.value.TransformValue.scale, label = "Scale: "};
                    scaleValue.RegisterValueChangedCallback(evt3 => {
                        prop.property.value.TransformValue.scale = evt3.newValue;
                    });
                    var rotationValue = new Vector3Field(){value = prop.property.value.TransformValue.scale, label = "Rotation: "};
                    rotationValue.RegisterValueChangedCallback(evt3 => {
                        prop.property.value.TransformValue.rotation = evt3.newValue;
                    });
                    var container = new VisualElement();
                    propertyElements.Add(container);
                    container.Add(positionValue);
                    container.Add(scaleValue);
                    container.Add(rotationValue);
                    propertiesSection.Add(new BlackboardRow(blackboardField, container));
                    break;
            }
        }
        void AddBehaviourToBlackboard(StateBehaviour behaviour, int index = 0){
            var blackboardField = new BlackboardField(){
                text = behaviour.name,
                typeText = "Behaviour",
            };
            blackboardField.userData = blackboardField.userData = new BlackboardFieldPropertyData(){
                type = BlackboardFieldPropertyData.FieldType.Behaviour,
                index =  index
            };
            behavioursSection.Add(blackboardField);
        }

        void ParameterCreationHandler<T>(object targetObject) where T : Parameter {
            var newParameter = ScriptableObject.CreateInstance<T>();
            var listOfParameters = new List<string>();
            parameters.ForEach(p => {
                listOfParameters.Add(p.parameterName);
            });
            newParameter.name = ObjectNames.GetUniqueName(listOfParameters.ToArray(), typeof(T).Name);
            main.AddData(newParameter);
            main.GenerateListOfParameters();
            var newWrapper = new ParameterWrapper(){
                parameter = newParameter,
                parameterName = newParameter.name
            };
            parameters.Add(newWrapper);
            main.GenerateListOfParameters();
            AssetDatabase.SaveAssets();
            GenerateBlackboard();
        }
        void PropertyCreationHandler<T>(object targetObject) where T : ExposedProperty {
            var newProperty = ScriptableObject.CreateInstance<T>();
            var listOfProperties = new List<string>();
            properties.ForEach(p => {
                listOfProperties.Add(p.propertyName);
            });
            newProperty.name = ObjectNames.GetUniqueName(listOfProperties.ToArray(), typeof(T).Name); 
            main.AddData(newProperty);
            var newWrapper = new ExposedPropertyWrapper(){
                property = newProperty,
                propertyName = newProperty.name
            };
            properties.Add(newWrapper);
            AssetDatabase.SaveAssets();
            GenerateBlackboard();
        }
        void BehaviourCreationHandler(object targetObject){
            var type = targetObject as System.Type;
            var newBehaviour = ScriptableObject.CreateInstance(type) as StateBehaviour;
            newBehaviour.name = newBehaviour.GetType().Name;
            main.AddData(newBehaviour);
            behaviours.Add(newBehaviour);
            AssetDatabase.SaveAssets();
            GenerateBlackboard();
        }
        void CreateBehaviourHandler(object targetObject) {
            var settings = AssetDatabase.LoadAssetAtPath<FSMSettings>(pathToSettings);
            var pathToScript = AssetDatabase.GenerateUniqueAssetPath(settings.PathToBehaviours + "NewStateBehaviour.cs");
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(settings.PathToTemplate);
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(settings.PathToTemplate, System.IO.Path.GetFileName(pathToScript));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(pathToScript);
        }
        void OnDisable() {
            StateMachineRuntime.OnParameterChanged -= ParameterChangeHandler;
        }
        void AddSearchWindow() {
            searchWindow = ScriptableObject.CreateInstance<GraphNodeSearchWindow>();
            searchWindow.Init(this, window);
            nodeCreationRequest = context => SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), searchWindow);
        }
        internal State CreateNode(System.Type type, Vector2 position = default) {
            var state = controller.CreateState(type, position);
            AddElement(new StateMachineNode(state, this));
            return state;
        }
        internal void CreateSubStateMachine(System.Type type, Vector2 position = default){
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
        internal void ChangeController(StateMachineControllerBase newController) {
            controller = newController;
            if (newController is StateMachineController) main = newController as StateMachineController;
            PopulateGraph();
        }
        void ReAssignOwners() {
            controller.states.ForEach(s => {
                s.Owner = controller;
            });
        }
        internal void PopulateGraph() {
            graphViewChanged -= OnGraphViewChanged;
            DeleteElements(graphElements.ToList());
            graphViewChanged += OnGraphViewChanged;
            TransitionMap =  new Dictionary<Edge, (State parent, State child)>();
            controller?.states?.ForEach(node => AddElement(new StateMachineNode(node, this)));
            controller?.subStates?.ForEach(node => AddElement(new SubStateMachineNode(node, this)));
            controller?.states?.ForEach(node => {
                var children = controller.GetTransitions(node);
                var parentView = GetNodeByGuid(node.guid) as GraphNode;
                foreach (var child in children) {
                    if (child.stateToTransition != null) {
                        var childView = GetTargetNodeFromTransition(child);
                        var edge = parentView?.output?.ConnectTo(childView.input);
                        AddTransitionEdge(edge, (node, child.stateToTransition));
                    }
                }
            });
            controller?.subStates?.ForEach(subState => {
                subState.GetTransitionableStates().ForEach(s => {
                    var transitions = subState.GetTransitions(s);
                    var parentView = GetNodeByGuid(subState.guid) as SubStateMachineNode;
                    transitions.ForEach(transition => {
                        var childView = GetTargetNodeFromTransition(transition);
                        Edge edge = null;
                        parentView?.subStatePorts.ForEach(p => {
                            if (p.portName == s.stateName && childView != parentView) {
                                edge = childView != null ? p.ConnectTo(childView.input) : null;
                            }
                        });
                        AddTransitionEdge(edge, (s, transition.stateToTransition));
                    });
                });
            });
        }
        GraphNode GetTargetNodeFromTransition(StateTransition transition) {
            var childView = GetNodeByGuid(transition.stateToTransition.guid) as GraphNode;
            if (childView == null) {
                if (controller.states.Any(s => s is UpState))
                    childView = GetNodeByGuid(controller.states.First(s => s is UpState)?.guid) as GraphNode;
                else {
                    childView = GetNodeByGuid(transition?.TargetStateMachine?.GetEntryState().guid) as GraphNode;
                }
            }
            return childView;
        }

        void AddTransitionEdge(Edge edge, (State parent, State child) states) {
            if (edge == null) {
                return;
            }
            if (edge.output.node == edge.input.node) return;
            TransitionMap.Add(edge, states);
            AddElement(edge);
        }
        internal void UpdateNodeNames() {
            nodes.ToList().ForEach(n => {
                switch (n) {
                    case StateMachineNode s:
                        if (s.title == s.state.name) return;
                        s.title = s.state.name;
                        s.state.stateName = s.state.name;
                        return;
                    case SubStateMachineNode s:
                        if (s.title == s.subStateMachine.name) return;
                        s.title = s.subStateMachine.name;
                        return;
                }
            });
        }
        internal void UpdateGraphElementPositions() {
            if (inspectorPanel.PositionHasChanged) {
                inspectorBorders = GetBorders(window.position, inspectorPanel.GetPosition());
            }
            if (BlackboardPositionChanged) {
                blackboardBorders = GetBorders(window.position, blackboard.GetPosition());
            }
        }
        internal void FitGraphElementsInCanvas(Rect canvas) {
            FitGraphElementInCanvas(canvas, inspectorPanel, ref inspectorBorders);
            FitGraphElementInCanvas(canvas, blackboard, ref blackboardBorders);
        }
        internal void FitGraphElementInCanvas(Rect canvas, GraphElement element, ref Vector4 borders) {
            if (element == null) return;
            var elementRect = element.GetPosition(); 
            if (elementRect.height >= canvas.height) return;
            var newPosition = new Vector2(
                borders switch {
                    _ when borders.x < borders.z => canvas.width - elementRect.width - borders.x,
                    _ when borders.x > borders.z => borders.z,
                    _ => (canvas.width - elementRect.width) * 0.5f
                },
                borders switch {
                    _ when borders.y < borders.w => canvas.height - elementRect.height - borders.y,
                    _ when borders.y > borders.w => borders.w, 
                    _ => (canvas.height - elementRect.height) * 0.5f
                }
            );
            element.SetPosition(new Rect(newPosition, elementRect.size)); 
            borders = GetBorders(window.position, elementRect);
        }
        Vector4 GetBorders(Rect canvas, Rect window) {
            return new Vector4() {
                x = canvas.width - window.xMax,
                y = canvas.height - window.yMax,
                z = window.xMin,
                w = window.yMin
            };
        }
        internal void SetNodeHighlight() {
            if (main?.CurrentState == null) return;
            var previousNode = GetNodeByGuid(currentSelectedGuid) as GraphNode;
            var node = GetNodeByGuid(main.CurrentState.guid) as GraphNode;
            if (node == null) {
                var subStateGuid = main.CurrentState.Owner.GetEntryState().guid;
                node = GetNodeByGuid(subStateGuid) as GraphNode;
                if (node == null) {
                    if (previousNode != null && previousNode.highlighted) {
                        previousNode.highlighted = false;
                        previousNode.style.backgroundColor = Color.clear;
                    }
                    return;
                }
            }
            if (previousNode == null && main.PreviousState != null) {
                var subStateGuid = main.PreviousState.Owner.GetEntryState().guid;
                previousNode = GetNodeByGuid(subStateGuid) as GraphNode;
            }
            if (previousNode != null && previousNode.highlighted) {
                previousNode.highlighted = false;
                previousNode.style.backgroundColor = Color.clear;
            }
            if (node.highlighted) return;
            currentSelectedGuid = main.CurrentState.guid;
            node.style.borderBottomLeftRadius = 8;
            node.style.borderBottomRightRadius = 8;
            node.style.borderTopLeftRadius = 8;
            node.style.borderTopRightRadius = 8;
            node.style.backgroundColor = new Color(0.75f, 0.75f, 1, 0.8f);
            node.highlighted = true;
        }
        GraphViewChange OnGraphViewChanged (GraphViewChange change) {
            if (change.elementsToRemove != null) {
                change.elementsToRemove.ForEach(element => {
                    switch (element) {
                        case BlackboardField f:
                            DeleteElementFromBlackboard((BlackboardFieldPropertyData)element.userData);
                            GenerateBlackboard();
                            break;
                        case StateMachineNode stateMachineNode:
                            controller.DeleteState(stateMachineNode.state);
                            SetInspectorDisplay(null);
                            break;
                        case SubStateMachineNode subStateMachineNode:
                            controller.DeleteSubStateMachine(subStateMachineNode.subStateMachine);
                            window?.AddSubStatesToMenu();
                            SetInspectorDisplay(null);
                            break;
                        case Edge edge:
                            var parent = edge.output.node;
                            var child = edge.input.node;
                            TransitionMap.Remove(edge);
                            controller.DeleteChild(
                                parent is StateMachineNode
                                    ? (parent as StateMachineNode).state
                                    : (parent as SubStateMachineNode).subStateMachine.states.First(s => s.stateName == edge.output.portName),
                                child is StateMachineNode
                                    ? (child as StateMachineNode).state
                                    : (child as SubStateMachineNode).subStateMachine.GetEntryState()
                            );
                            SetInspectorDisplay(null);
                            break;
                        default:
                            break;
                    }
                });
            }
            if (change.edgesToCreate != null) {
                change.edgesToCreate.ForEach(edge => {
                    var parent = edge.output.node;
                    var parentState = parent is StateMachineNode
                        ? (parent as StateMachineNode).state
                        : (parent as SubStateMachineNode).subStateMachine.states.First(s => s.stateName == edge.output.portName);
                    var child = edge.input.node;
                    if (child is StateMachineNode) {
                        var n = child as StateMachineNode;
                        var anyState = n.state as UpState;
                        if (anyState != null) {
                            var menu = new GenericMenu();
                            (controller as SubStateMachineController).parent.states.ForEach(s => {
                                menu.AddItem(new GUIContent(s.stateName), false, EdgeCreationHandler, new StateContainer(parent, s, edge));
                            });
                            menu.ShowAsContext();
                            return;
                        } 
                    } else if (child is SubStateMachineNode) {
                        var n = child as SubStateMachineNode;
                        var entryState = n.subStateMachine.GetEntryState();
                        if (entryState != null) {
                            var menu = new GenericMenu();
                            n.subStateMachine.states.ForEach(s => {
                                if (!(s is AnyState) && !(s is UpState) && !(s is ExitState) && !(s is EntryState))
                                    menu.AddItem(new GUIContent("States/" + s.stateName), false, EdgeCreationHandler, new StateContainer(parent, s, edge));
                            });
                            
                            menu.AddItem(new GUIContent("State Machines/" + n.subStateMachine.name), false, EdgeCreationHandler, new StateContainer(parent, entryState, edge));
                            n.subStateMachine.subStates.ForEach(s => {
                                menu.AddItem(new GUIContent("State Machines/" + s.name), false, EdgeCreationHandler, new StateContainer(parent, s.GetEntryState(), edge));
                            });
                            menu.ShowAsContext();
                            return;
                        } 
                    }
                    OnEdgeCreationAddChild(edge, parentState, child is StateMachineNode ? (child as StateMachineNode).state : (child as SubStateMachineNode).subStateMachine.GetEntryState());
                });
            }
            return change;
        }
        class StateContainer {
            public Node parent;
            public Edge edge;
            public State child;
            public StateContainer (Node parent, State child, Edge edge) {
                this.parent = parent;
                this.child = child; 
                this.edge = edge;
            }
        }
        void EdgeCreationHandler(object target) {
            var container = target as StateContainer;
            var parentState = container.parent is StateMachineNode
                ? (container.parent as StateMachineNode).state
                : (container.parent as SubStateMachineNode).subStateMachine.states.First(s => s.stateName == container.edge.output.portName);
            OnEdgeCreationAddChild(container.edge, parentState, container.child, true);
        }
        void OnEdgeCreationAddChild(Edge edge, State parent, State child, bool outward = false) {
            TransitionMap.Add(edge, (parent, child));
            controller.AddChild(parent, child, outward);
        }

    }
}