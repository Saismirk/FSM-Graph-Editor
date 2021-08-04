#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEditor;
#endif
using UnityEngine;
namespace FSM.Graph {
    public class StateMachineNode : GraphNode {
        public State state;
        public StateMachineNode (State state) {
            this.state = state;
            title = state.stateName;
            viewDataKey = state.guid;

            style.left = state.position.x;
            style.top = state.position.y;

            capabilities |= Capabilities.Renamable;
            capabilities |= Capabilities.Copiable;

            switch (state) {
                case EntryState s:
                    CreateOutputPort("", Port.Capacity.Single);
                    break;
                case ExitState s:
                    CreateInputPort("");
                    break;
                default:
                    CreateInputPort();
                    CreateOutputPort();
                    break;
            }
        }
        public override void SetPosition(Rect newPos) {
            base.SetPosition(newPos);
            Undo.RecordObject(state, "Node Change (Position)");
            state.position.Set(newPos.xMin, newPos.yMin);
            if (title != state.stateName) title = state.stateName;
            EditorUtility.SetDirty(state);
        }
        public override void OnSelected() {
            base.OnSelected();
            if (state != null) Selection.activeObject = state;
            if (title != state.stateName) title = state.stateName;
        }
    }
}
