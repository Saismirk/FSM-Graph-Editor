#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor;
#endif
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
namespace FSM.Graph {
    internal class SubStateMachineNode : GraphNode {
        internal SubStateMachineController subStateMachine;
        StateMachineGraphView graphView;
        internal List<Port> subStatePorts =  new List<Port>();
        internal SubStateMachineNode (SubStateMachineController subStateMachine, StateMachineGraphView graphView) {
            this.subStateMachine = subStateMachine;
            title = subStateMachine.name;
            viewDataKey = subStateMachine.guid;
            this.graphView = graphView;

            style.left = subStateMachine.position.x;
            style.top = subStateMachine.position.y;

            capabilities |= Capabilities.Renamable | Capabilities.Copiable | Capabilities.Collapsible | Capabilities.Movable | Capabilities.Deletable | Capabilities.Snappable;

            var enterSubStateButton = new Button(() => {
                this.graphView.ChangeController(subStateMachine);
                this.graphView.window.AddSubStateLayerButton(subStateMachine);
            }){
                text = "Enter Sub-State"
            };
            mainContainer.Add(enterSubStateButton);
            CreateInputPort("Entry");
            for (var i = 0; i < subStateMachine.states.Count; i++) {
                var s = subStateMachine.states[i];
                if (s is EntryState || s is AnyState || s is UpState) continue;
                CreateSubStatePort(s?.stateName);
            }
        }
        public override void SetPosition(Rect newPos) {
            base.SetPosition(newPos);
            Undo.RecordObject(subStateMachine, "Node Change (Position)");
            subStateMachine.position.Set(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(subStateMachine);
        }
        protected void CreateSubStatePort(string portName = "Output", Port.Capacity capacity = Port.Capacity.Multi) {
            var port = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(float));
            port.portName = portName;
            outputContainer.Add(port);
            subStatePorts.Add(port);
            RefreshPorts();
            RefreshExpandedState(); 
        }
        public override void OnSelected() {
            base.OnSelected();
            if (subStateMachine != null) {
                graphView?.SetInspectorDisplay(subStateMachine);
            }
        }
    }   
}
