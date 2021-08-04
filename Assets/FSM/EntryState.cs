#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public class EntryState : State {
        #if UNITY_EDITOR
        [CustomEditor(typeof(EntryState))]
        public class EntryStateEditor : Editor {
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("Entry State", EditorStyles.boldLabel);
            }
        }
        #endif
    }
}

