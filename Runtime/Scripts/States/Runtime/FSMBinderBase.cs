using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace FSM {
    [ExecuteInEditMode]
    public abstract class FSMBinderBase : MonoBehaviour {
        protected StateMachineRuntime binder;
        public bool Initialized {get; private set;}
        public abstract bool IsValid(StateMachineRuntime component);
        public abstract void UpdateBinding(StateMachineRuntime component);
        protected virtual void Awake() {
            binder = GetComponent<StateMachineRuntime>();
            Initialized = true;
        }
        protected virtual void OnEnable() {
            if (!binder.m_Bindings.Contains(this))
                binder.m_Bindings.Add(this);
            hideFlags = HideFlags.HideInInspector;
        }
        protected virtual void OnDisable() {
            if (binder.m_Bindings.Contains(this))
                binder.m_Bindings.Remove(this);
        }
        public override string ToString() {
            return GetType().ToString();
        }
    }
    #if UNITY_EDITOR
    class FSMBinderEditor : Editor {}
    #endif
}