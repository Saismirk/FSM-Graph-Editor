using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public abstract class StateMachineControllerBase : ScriptableObject {
        public List<State> states = new List<State>();
        public List<SubStateMachineController> subStates = new List<SubStateMachineController>();
        public string currentState, previousState;
        public int depth = 0;
        public State CreateState(System.Type type, Vector2 position = default, bool undo = true) {
            if (undo) Undo.RecordObject(this, "Creation (State)");
            var state = CreateInstance(type) as State;
            state.hideFlags = HideFlags.HideInHierarchy;
            state.stateName = type.Name;
            state.name = type.Name;
            state.guid = GUID.Generate().ToString();
            state.position = position;
            state.Init(this is StateMachineController ? this as StateMachineController : (this as SubStateMachineController).main);
            states.Add(state);
            

            AssetDatabase.AddObjectToAsset(state, this);
            if (undo) Undo.RegisterCreatedObjectUndo(state, "Creation (State)");
            AssetDatabase.SaveAssets();
            return state;
        }

        public SubStateMachineController CreateSubStateMachine(System.Type type, Vector2 position = default) {
            Undo.RecordObject(this, "Creation (State)");
            var subStateMachine = CreateInstance(type) as SubStateMachineController;
            //subStateMachine.hideFlags = HideFlags.HideInHierarchy;
            subStateMachine.name = type.Name;
            subStateMachine.guid = GUID.Generate().ToString();
            subStateMachine.position = position;
            subStates.Add(subStateMachine);

            AssetDatabase.AddObjectToAsset(subStateMachine, this);
            Undo.RegisterCreatedObjectUndo(subStateMachine, "Creation (State)");
            AssetDatabase.SaveAssets();
            subStateMachine.Init(
                this is SubStateMachineController ? (this as SubStateMachineController).main : this as StateMachineController,
                this
            );
            return subStateMachine;
        }
        public void DeleteState(State state) {
            Undo.RecordObject(this, "Deletion (State)");
            states.Remove(state);
            Undo.DestroyObjectImmediate(state);
            AssetDatabase.SaveAssets();
        }
        public void DeleteSubStateMachine(SubStateMachineController subStateMachine) {
            Undo.RecordObject(this, "Deletion (StateMachine)");
            subStateMachine.DeleteAllStates();
            subStates.Remove(subStateMachine);
            Undo.DestroyObjectImmediate(subStateMachine);
            AssetDatabase.SaveAssets();
        }
        public void DeleteAllStates() {
            Undo.RecordObject(this, "Deletion (States)");
            states.ForEach(state => {
                AssetDatabase.RemoveObjectFromAsset(state);
            });
            states.Clear();
            while(subStates.Count > 0) {
                if (subStates[0] == null) break;
                DeleteSubStateMachine(subStates[0]);
            }
            subStates.Clear();
            AssetDatabase.SaveAssets();
        }
        public void AddChild(State parent, State child){
            parent?.AddTransition(child);
        }
        public void DeleteChild(State parent, State child) {
            parent.RemoveTransition(parent.transitions.Where(t => t.stateToTransition == child).First());
        }
        public List<StateTransition> GetTransitions(State parent) {
            return parent.transitions;
        }
        public EntryState GetEntryState() {
            var entry = states?.Where(state => state is EntryState)?.ToList();
            if (entry != null && entry.Any()) return entry.First() as EntryState;
            return null;
        }
        public void GetParents(ref List<StateMachineControllerBase> parents) {
            if (this is SubStateMachineController) {
                parents.Add((this as SubStateMachineController).parent);
                (this as SubStateMachineController).parent.GetParents(ref parents);
            }
        }
        public ExitState GetExitState() {
            var entry = states?.Where(state => state is ExitState)?.ToList();
            if (entry != null && entry.Any()) return entry.First() as ExitState;
            return null;
        }
        void InitializeStates(StateMachineController controller) {
            states.ForEach(state => {
                state.Init(controller);
                state.transitions.ForEach(t => {
                    t.Init(controller);
                    t.conditions.ForEach(c => c.Init(controller));
                });
            });
        }
        public void InitalizeControllers(StateMachineController controller) {
            InitializeStates(controller);
            subStates.ForEach(sub => {
                sub.InitalizeControllers(controller);
            });
        }

        
    }
}