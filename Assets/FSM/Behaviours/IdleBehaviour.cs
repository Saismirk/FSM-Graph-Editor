using UnityEngine;
namespace FSM {
    public class IdleBehaviour : StateBehaviour {
        public float attackTimer = 2;
        public override void OnUpdate(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {
            stateMachineController.SetBool("detected", true);

        }
    }
}

