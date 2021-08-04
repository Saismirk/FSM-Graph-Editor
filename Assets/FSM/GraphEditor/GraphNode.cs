#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
namespace FSM.Graph {
    public abstract class GraphNode : Node {
        public string guid;
        public Port input, output;
        protected void CreateInputPort(string portName = "Input", Port.Capacity capacity = Port.Capacity.Multi) {
            input = InstantiatePort(Orientation.Horizontal, Direction.Input, capacity, typeof(float));
            input.portName = portName;
            inputContainer.Add(input);
            RefreshPorts();
            RefreshExpandedState();
        }
        protected void CreateOutputPort(string portName = "Output", Port.Capacity capacity = Port.Capacity.Multi) {
            output = InstantiatePort(Orientation.Horizontal, Direction.Output, capacity, typeof(float));
            output.portName = portName;
            outputContainer.Add(output);
            RefreshPorts();
            RefreshExpandedState(); 
        }
    }
}
