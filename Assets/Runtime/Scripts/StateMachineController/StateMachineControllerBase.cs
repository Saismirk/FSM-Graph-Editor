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
            var stateNames = new List<string>();
            states.ForEach(s => {
                stateNames.Add(s.name);
            });
            state.stateName = ObjectNames.GetUniqueName(stateNames.ToArray(), type.Name);
            state.name = state.stateName;
            state.guid = GUID.Generate().ToString();
            state.position = position;
            state.Init(this is StateMachineController ? this as StateMachineController : (this as SubStateMachineController).main, this);
            states.Add(state);
            if (state is AnyState) {
                if (this is StateMachineController) (this as StateMachineController).anyStates.Add(state as AnyState);
                else (this as SubStateMachineController).main.anyStates.Add(state as AnyState);
            }
            AssetDatabase.AddObjectToAsset(state, this);
            if (undo) Undo.RegisterCreatedObjectUndo(state, "Creation (State)");
            AssetDatabase.SaveAssets();
            return state;
        }
        internal void CloneStateBehaviours() {
            states.ForEach(s => {
                s.CloneBehaviours();
            });
            subStates.ForEach(s => {
                s.CloneStateBehaviours();
            });
        }
        internal abstract string GetPath(bool noSelf);

        internal List<State> GetTransitionableStates() {
            var list = new List<State>();
            return states.Where(s => !(s is UpState) && !(s is EntryState) && !(s is AnyState)).ToList();
        }

        public SubStateMachineController CreateSubStateMachine(System.Type type, Vector2 position = default) {
            Undo.RecordObject(this, "Creation (State)");
            var subStateMachine = CreateInstance(type) as SubStateMachineController;
            var listofSubstates = new List<string>();
            subStates.ForEach(s => {
                listofSubstates.Add(s.name);
            });
            subStateMachine.name = ObjectNames.GetUniqueName(listofSubstates.ToArray(), type.Name);
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
        internal void GetAndAddAllAnyStates(ref List<AnyState> list) {
            list.Add(states.First(s => s is AnyState) as AnyState);
            for (var i = 0; i < subStates.Count; i++) {
                subStates[i].GetAndAddAllAnyStates(ref list);
            }
        }
        internal void DeleteState(State state) {
            Undo.RecordObject(this, "Deletion (State)");
            states.Remove(state);
            if (state is AnyState) {
                if (this is StateMachineController) (this as StateMachineController).anyStates.Remove(state as AnyState);
                else (this as SubStateMachineController).main.anyStates.Remove(state as AnyState);
            }
            Undo.DestroyObjectImmediate(state);
            AssetDatabase.SaveAssets();
        }
        internal void DeleteSubStateMachine(SubStateMachineController subStateMachine) {
            Undo.RecordObject(this, "Deletion (StateMachine)");
            subStateMachine.DeleteAllStates();
            subStates.Remove(subStateMachine);
            Undo.DestroyObjectImmediate(subStateMachine);
            AssetDatabase.SaveAssets();
        }
        internal void DeleteAllStates() {
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
        internal void AddChild(State parent, State child, bool outward = false){
            parent?.AddTransition(child, outward);
        }
        internal void DeleteChild(State parent, State child) {
            var transition = parent.transitions.Any(t => t.stateToTransition == child)
                ? parent.transitions.First(t => t.stateToTransition == child)
                : parent.transitions.First(t => t.outwardTransition);
            parent.RemoveTransition(transition);
        }
        internal List<StateTransition> GetTransitions(State parent) {
            return parent.transitions;
        }
        internal EntryState GetEntryState() {
            var entry = states?.Where(state => state is EntryState)?.ToList();
            if (entry != null && entry.Any()) return entry.First() as EntryState;
            return null;
        }
        internal void GetParents(ref List<StateMachineControllerBase> parents) {
            if (this is SubStateMachineController) {
                parents.Add((this as SubStateMachineController).parent);
                (this as SubStateMachineController).parent.GetParents(ref parents);
            }
        }
        internal void GetAndAddAllSubStates(ref List<StateMachineControllerBase> list) {
            for (var i = 0; i < subStates.Count; i++) {
                list.Add(subStates[i]);
                subStates[i].GetAndAddAllSubStates(ref list);
            }
        }
        internal ExitState GetExitState() {
            var entry = states?.Where(state => state is ExitState)?.ToList();
            if (entry != null && entry.Any()) return entry.First() as ExitState;
            return null;
        }
        void InitializeStates(StateMachineController controller) {
            states.ForEach(state => {
                state.Init(controller, this);
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
        public virtual void GenerateListOfParameters(List<ParameterWrapper> paramList, ref string[] listOfParameters) {
            if (Application.isPlaying) return;
            var list = new List<string>();
            paramList.ForEach(p => {
                if (p.parameter != null) list.Add(p.parameter.name);
            });
            listOfParameters = list.ToArray();
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        public virtual void GenerateListOfParameters() {}

    }
}