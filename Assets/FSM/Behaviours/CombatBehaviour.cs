namespace FSM {
    public class CombatBehaviour : StateBehaviour {
        public float idlePoseChangeTimer = 2;
        public override void OnUpdate(StateMachineRuntime stateMachine, StateMachineController stateMachineController, State state) {
            stateMachineController.SetBool("detected", false);
        }
    }
}

