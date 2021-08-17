using UnityEngine;
#if UNITY_EDITOR
#endif
namespace FSM {
    [AddComponentMenu("FSM/Property Binders/Transform Binder")]
    [FSMBinder("Transform/Transform")]
    public class FSMTransformBinder : FSMBinderBase {
        protected TransformProperty property;
        public string propertyName;
        public Transform Target = null;
        public override void UpdateBinding(StateMachineRuntime component) {
            component.SetTransformProperty(propertyName, Target);
        }
        public override bool IsValid(StateMachineRuntime component) {
            if (string.IsNullOrEmpty(propertyName)) return false;
            return component.HasTransform(propertyName.GetHashCode()); 
        }
        public override string ToString() {
            return "Transform Binder";
        }
    }
}