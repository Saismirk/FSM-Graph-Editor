using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public class SubStateMachineController : StateMachineControllerBase {
        public StateMachineController main;
        public StateMachineControllerBase parent;
        public string subStateName;
        public string guid;
        public Vector2 position;
        public void Init(StateMachineController main, StateMachineControllerBase parent) {
            this.parent = parent;
            depth = parent.depth + 1;
            this.main = main;
            var entry = CreateState(typeof(EntryState));
            entry.guid = guid;
            AssetDatabase.SaveAssets();
            CreateState(typeof(ExitState), new Vector2(300, 0));
        }

    #region Editor
    #if UNITY_EDITOR
        [CustomEditor(typeof(SubStateMachineController))]
        public class SubStateMachineControllerEditor : Editor {
            SubStateMachineController Controller => target as SubStateMachineController;
            public override void OnInspectorGUI() {
                EditorGUI.BeginChangeCheck();
                Controller.name = EditorGUILayout.DelayedTextField("SubState Name: ", Controller.name);
                if (EditorGUI.EndChangeCheck()) {
                    EditorUtility.SetDirty(this);
                    serializedObject.ApplyModifiedProperties();
                    AssetDatabase.SaveAssets();
                }
            }
        }
        #endif
    #endregion
    }
}