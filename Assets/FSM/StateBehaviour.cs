using UnityEngine;
namespace FSM {
    public abstract class StateBehaviour : ScriptableObject {
        public virtual void OnEnter(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
        public virtual void OnUpdate(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
        public virtual void OnExit(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
    }
}

