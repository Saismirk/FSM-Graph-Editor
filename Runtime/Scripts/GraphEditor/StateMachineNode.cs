#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor;
#endif
using UnityEngine;
namespace FSM.Graph {
    public class StateMachineNode : GraphNode {
        internal State state;
        StateMachineGraphView graphView;
        internal StateMachineNode (State state, StateMachineGraphView graphView = null) {
            this.state = state;
            this.graphView = graphView;
            title = state.stateName;
            viewDataKey = state.guid;

            style.left = state.position.x;
            style.top = state.position.y;

            switch (state) {
                case EntryState s:
                    CreateOutputPort("", Port.Capacity.Single);
                    break;
                case ExitState s:
                    CreateInputPort("");
                    break;
                case AnyState s:
                    CreateOutputPort("");
                    break;
                case UpState s:
                    CreateInputPort("");
                    break;
                default:
                    capabilities |= Capabilities.Renamable | Capabilities.Copiable;
                    CreateInputPort();
                    CreateOutputPort();
                    break;
            }
        }
        public override void SetPosition(Rect newPos) {
            base.SetPosition(newPos);
            Undo.RecordObject(state, "Node Change (Position)");
            state.position.Set(newPos.xMin, newPos.yMin);
            EditorUtility.SetDirty(state);
        }
        public override void OnSelected() {
            base.OnSelected();
            if (state != null) graphView?.SetInspectorDisplay(state);
        }
    }
}
