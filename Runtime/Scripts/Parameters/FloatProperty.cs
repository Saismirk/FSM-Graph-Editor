using System;
namespace FSM {
    [Serializable]
    public class FloatProperty : ExposedProperty {
        public FloatProperty() {
            value.type = PropertyValue.PropertyType.Float;
        }
        public override object GetValue() => value.FloatValue;
        public void SetValue(float value) {
            this.value.FloatValue = value;
        }
    }
}