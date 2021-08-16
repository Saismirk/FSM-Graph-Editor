using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public class SubStateMachineController : StateMachineControllerBase {
        public StateMachineController main;
        public StateMachineControllerBase parent;
        public string subStateName;
        public ExitState exitState => states.OfType<ExitState>().First();
        public string guid;
        public Vector2 position;
        public void Init(StateMachineController main, StateMachineControllerBase parent) {
            this.parent = parent;
            depth = parent.depth + 1;
            this.main = main;
            var entry = CreateState(typeof(EntryState));
            entry.guid = guid;
            AssetDatabase.SaveAssets();
            CreateState(typeof(ExitState), new Vector2(400, 20));
            CreateState(typeof(AnyState), new Vector2(20, 300));
            CreateState(typeof(UpState), new Vector2(400, 300));
        }

        internal override string GetPath(bool includeSelf) {
            var paths = new List<string>();
            if (includeSelf) paths.Add("/" + name);
            paths.Add(parent.GetPath(true));
            var path = string.Empty;
            paths.Reverse();
            paths.ForEach(p => {
                path += p;
            });
            return path;
        }

        #region Editor
#if UNITY_EDITOR
        [CustomEditor(typeof(SubStateMachineController))]
        public class SubStateMachineControllerEditor : Editor {
            SubStateMachineController Controller => target as SubStateMachineController;
            [SerializeField] List<StateTransition> transitions = new List<StateTransition>();
            private void OnEnable() {
                Controller.states.ForEach(s => {
                    s.transitions.ForEach(t => {
                        if (t.outwardTransition)
                            transitions.Add(t);
                    });
                });
            }
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("Depth: "+ Controller.depth);
                EditorGUILayout.LabelField("States Inside: " + Controller.states.Count);
                EditorGUILayout.LabelField("Sub-States Inside: " + Controller.subStates.Count);
                EditorGUILayout.LabelField("Outward Transitions", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                    int i = 0;
                    transitions.ForEach(t => {
                        var so = new SerializedObject(this);
                        EditorGUILayout.PropertyField(so.FindProperty("transitions").GetArrayElementAtIndex(i));
                        i++;
                    });
                EditorGUI.indentLevel--;
            }
        }
        #endif
    #endregion
    }
}