using System;
using UnityEngine;
namespace FSM {
    public abstract class StateBehaviour : ScriptableObject {
        public virtual void OnEnter(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
        public virtual void OnUpdate(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
        public virtual void OnExit(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {}
        public virtual void OnFSMControllerColliderHit(ControllerColliderHit hit, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMCollisionEnter(Collision collision, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMCollisionExit(Collision collision, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMCollisionStay(Collision collision, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMTriggerEnter(Collider collider, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMTriggerExit(Collider collider, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMTriggerStay(Collider collider, StateMachineRuntime stateMachine) {}
        public virtual void OnFSMAnimatorMove(StateMachineRuntime stateMachine) {}
    }
}

