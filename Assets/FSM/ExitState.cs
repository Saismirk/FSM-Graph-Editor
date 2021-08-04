#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public class ExitState : State {
        #if UNITY_EDITOR
        [CustomEditor(typeof(ExitState))]
        public class ExitStateEditor : Editor {
            public override void OnInspectorGUI() {
                EditorGUILayout.LabelField("Exit State", EditorStyles.boldLabel);
            }
        }
        #endif
    }
}

