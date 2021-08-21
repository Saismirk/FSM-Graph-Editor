using UnityEngine;
using System;
namespace FSM {

    [Serializable]
    public class TransformProperty : ExposedProperty {
        public TransformProperty() {
            value.type = PropertyValue.PropertyType.Transform;
        }
        public override object GetValue() => value.TransformValue;
        public void SetValue(TransformData value) {
            this.value.TransformValue = value;
        }
    }
}