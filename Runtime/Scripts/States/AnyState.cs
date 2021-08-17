#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    public class AnyState : State {
        #if UNITY_EDITOR
        [CustomEditor(typeof(AnyState))]
        public class AnyStateEditor : Editor {
            public override void OnInspectorGUI() {
                
            }
        }
        #endif
    }
}

