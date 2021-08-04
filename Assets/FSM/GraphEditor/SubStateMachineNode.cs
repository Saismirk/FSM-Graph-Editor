#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UIElements;
namespace FSM.Graph {
    public class SubStateMachineNode : GraphNode {
        public SubStateMachineController subStateMachine;
        StateMachineGraphView graphView;
        public SubStateMachineNode (SubStateMachineController subStateMachine, StateMachineGraphView graphView) {
            this.subStateMachine = subStateMachine;
            title = subStateMachine.name;
            viewDataKey = subStateMachine.guid;
            this.graphView = graphView;

            style.left = subStateMachine.position.x;
            style.top = subStateMachine.position.y;

            capabilities |= Capabilities.Renamable;
            capabilities |= Capabilities.Copiable;

            var enterSubStateButton = new Button(() => {
                this.graphView.ChangeController(subStateMachine);
                this.graphView.window.AddSubStateLayerButton(subStateMachine);
            }){
                text = "Enter Sub-State"
            };
            mainContainer.Add(enterSubStateButton);
            CreateInputPort();
            CreateOutputPort();
        }
        public override void SetPosition(Rect newPos) {
            base.SetPosition(newPos);
            Undo.RecordObject(subStateMachine, "Node Change (Position)");
            subStateMachine.position.Set(newPos.xMin, newPos.yMin);
            if (title != subStateMachine.name) title = subStateMachine.name;
            EditorUtility.SetDirty(subStateMachine);
        }
        public override void OnSelected() {
            base.OnSelected();
            if (subStateMachine != null) Selection.activeObject = subStateMachine;
            if (title != subStateMachine.name) title = subStateMachine.name;
        }
    }   
}
