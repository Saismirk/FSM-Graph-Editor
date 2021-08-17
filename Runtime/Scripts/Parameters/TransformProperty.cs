using UnityEngine;
using System;
[Serializable]
public class TransformProperty : ExposedProperty {
    public TransformProperty() {
        value.type = PropertyValue.PropertyType.Transform;
    }
    public override object GetValue() => value.TransformValue;
    public override void SetValue(PropertyValue value) {
        this.value = value;
    }
    public void SetValue(TransformData value) {
        this.value.TransformValue = value;
    }
}